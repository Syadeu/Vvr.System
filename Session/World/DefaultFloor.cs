#region Copyrights

// Copyright 2024 Syadeu
// Author : Seung Ha Kim
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// File created : 2024, 05, 10 20:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.Provider;
using Vvr.Session.World.Core;

namespace Vvr.Session.World
{
 	// [ParentSession(typeof(DefaultRegion), true)]
    public partial class DefaultFloor : ParentSession<DefaultFloor.SessionData>,
        IFloor,
        IConnector<IUserActorProvider>,

        IConnector<IStageViewProvider>,
        IConnector<IActorViewProvider>,
        IConnector<IContentViewEventHandlerProvider>
    {
        public struct SessionData : ISessionData
        {
            public readonly IEnumerable<IStageData> stages;
            public readonly IActor[]              existingActors;

            public SessionData(IEnumerable<IStageData> s, IActor[] existingActors)
            {
                stages              = s;
                this.existingActors = existingActors;
            }
        }

        public struct Result
        {
            public readonly IActor[] alivePlayerActors;

            public Result(IActor[] x)
            {
                alivePlayerActors = x;
            }
        }

        private DefaultStage          m_CurrentStage;

        private IAssetProvider     m_AssetProvider;
        private IUserActorProvider m_UserActorProvider;

        private IStageViewProvider       m_StageViewProvider;
        private IActorViewProvider m_ActorViewProvider;

        private IContentViewEventHandlerProvider m_ViewEventHandlerProvider;

        private int                     m_CurrentStageIndex;
        private CancellationTokenSource m_StageCancellationTokenSource;

        private GameConfigResolveSession
            m_FloorConfigResolveSession,
            m_StageConfigResolveSession;

        public override string DisplayName => nameof(DefaultFloor);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            // We dont need to manually close these sessions
            // When this session close, child session also closed.
            var timelineSession = await CreateSession<TimelineQueueSession>(default);
            m_AssetProvider = await CreateSession<AssetSession>(default);
            var stageActorCreateSession = await CreateSession<StageActorFactorySession>(default);

            m_FloorConfigResolveSession = await CreateSession<GameConfigResolveSession>(
                new GameConfigResolveSession.SessionData(MapType.Floor, false));
            m_StageConfigResolveSession = await CreateSession<GameConfigResolveSession>(
                new GameConfigResolveSession.SessionData(MapType.Stage, false));

            Connect<IGameMethodProvider>(m_FloorConfigResolveSession)
                .Connect<IGameMethodProvider>(m_StageConfigResolveSession);

            // Register providers to inject child sessions.
            Register<ITimelineQueueProvider>(timelineSession)
                .Register<IAssetProvider>(m_AssetProvider)
                .Register<IStageActorProvider>(stageActorCreateSession)
                ;
            Vvr.Provider.Provider.Static.Register<IFloor>(this);

            await base.OnInitialize(session, data);
        }

        protected override async UniTask OnReserve()
        {
            m_StageCancellationTokenSource?.Cancel();
            m_StageCancellationTokenSource?.Dispose();

            Disconnect<IGameMethodProvider>(m_FloorConfigResolveSession)
                .Disconnect<IGameMethodProvider>(m_StageConfigResolveSession);

            Unregister<ITimelineQueueProvider>()
                .Unregister<IAssetProvider>()
                .Unregister<IStageActorProvider>()
                ;
            Vvr.Provider.Provider.Static.Unregister<IFloor>(this);

            m_AssetProvider              = null;
            m_FloorConfigResolveSession = null;
            m_StageConfigResolveSession = null;

            await base.OnReserve();
        }

        public async UniTask<Result> Start()
        {
            using var evMethod = ConditionTrigger.Scope(m_FloorConfigResolveSession.Resolve, nameof(m_FloorConfigResolveSession));
            using var trigger  = ConditionTrigger.Push(this, DisplayName);

            Started             = true;
            WasStartedOnce      = true;
            m_CurrentStageIndex = 0;

            List<IActor> prevPlayers = new(Data.existingActors);

            int        stageCount            = Data.stages.Count();
            IStageData cachedStartStage = Data.stages.First();
            await BeforeStageStartAsync(cachedStartStage, 0, stageCount);

            await trigger.Execute(Model.Condition.OnFloorStarted, $"{cachedStartStage.Floor}");

            // TODO: test
            await m_StageViewProvider.OpenEntryViewAsync(
                "테스트 필드", $"제 {cachedStartStage.Floor} 층")
                    .AttachExternalCancellation(ReserveToken)
                ;
            await UniTask.WaitForSeconds(2);
            await m_StageViewProvider.CloseEntryViewAsync().AttachExternalCancellation(ReserveToken);

            int       count         = 0;
            using var stageIterator = Data.stages.GetEnumerator();
            bool      hasNext       = stageIterator.MoveNext();
            while (hasNext)
            {
                IStageData stage = stageIterator.Current;

                if (count++ > 0)
                    await BeforeStageStartAsync(stage, count - 1, stageCount);

                using (var stageCancellationTokenSource = new CancellationTokenSource())
                {
                    m_StageCancellationTokenSource = stageCancellationTokenSource;

                    await StartStage(stage, prevPlayers, m_StageCancellationTokenSource.Token)
                        .AttachExternalCancellation(ReserveToken);

                    "end stage".ToLog();
                    await UniTask.Yield();

                    if (m_StageCancellationTokenSource.IsCancellationRequested ||
                        !prevPlayers.Any())
                    {
                        foreach (var actor in prevPlayers)
                        {
                            await m_ActorViewProvider.ReleaseAsync(actor)
                                    .AttachExternalCancellation(ReserveToken)
                                ;
                            actor.Release();
                        }

                        prevPlayers.Clear();
                        "restart stage".ToLog();
                        continue;
                    }
                    "2 end Stage".ToLog();
                }

                "Stage cleared".ToLog();

                hasNext = stageIterator.MoveNext();
            }

            var floorResult = new Result(
                prevPlayers.Any() ? prevPlayers.ToArray() : Array.Empty<IActor>());

            m_CurrentStage = null;
            Started        = false;

            await trigger.Execute(Model.Condition.OnFloorEnded, $"{cachedStartStage.Floor}");

            return floorResult;
        }

        public void RestartStage()
        {
            m_StageCancellationTokenSource?.Cancel();
        }

        private async UniTask StartStage(
            IStageData stage, List<IActor> prevPlayers, CancellationToken cancellationToken)
        {
            "start stage".ToLog();

            DefaultStage.SessionData sessionData;
            if (prevPlayers.Count == 0)
            {
                sessionData = new DefaultStage.SessionData(stage,
                    m_UserActorProvider.GetCurrentTeam());
            }
            else
            {
                sessionData = new DefaultStage.SessionData(stage,
                    prevPlayers);
            }

            using var trigger = ConditionTrigger.Push(this, DisplayName);
            using (ConditionTrigger.Scope(OnConditionTriggered, nameof(OnConditionTriggered)))
            using (ConditionTrigger.Scope(m_StageConfigResolveSession.Resolve, nameof(m_StageConfigResolveSession)))
            {
                m_CurrentStage = await CreateSession<DefaultStage>(sessionData);
                {
                    await trigger.Execute(Model.Condition.OnStageStarted, sessionData.stage.Id, ReserveToken);
                    DefaultStage.Result stageResult = await m_CurrentStage.Start(cancellationToken)
                        .AttachExternalCancellation(ReserveToken);
                    await trigger.Execute(Model.Condition.OnStageEnded, sessionData.stage.Id, ReserveToken);
                    m_CurrentStageIndex++;

                    foreach (var enemy in stageResult.enemyActors)
                    {
                        await m_ActorViewProvider.ReleaseAsync(enemy)
                                .AttachExternalCancellation(ReserveToken)
                            ;
                        enemy.Release();
                    }

                    prevPlayers.Clear();
                    prevPlayers.AddRange(stageResult.playerActors);
                }
                await m_CurrentStage.Reserve();
            }
        }

        private async UniTask BeforeStageStartAsync(IStageData stageData, int index, int count)
        {
            var backgroundImg
                = await m_AssetProvider.LoadAsync<Sprite>(stageData.Assets[AssetType.BackgroundImage])
                    .AttachExternalCancellation(ReserveToken);

            if (backgroundImg is not null)
            {
                m_ViewEventHandlerProvider.Resolve<WorldBackgroundViewEvent>()
                    .ExecuteAsync(
                        WorldBackgroundViewEvent.Open,
                        new WorldBackgroundViewEventContext(DisplayName, backgroundImg.Object))
                    .AttachExternalCancellation(ReserveToken)
                    .Forget();
            }

            var mainmenuViewHandler
                = m_ViewEventHandlerProvider.Resolve<MainmenuViewEvent>();

            mainmenuViewHandler.ExecuteAsync(MainmenuViewEvent.SetStageText,
                    // $"{stageData.Name} {stageData.Floor}-{index}"
                    $"{stageData.Name}"
                )
                .AttachExternalCancellation(ReserveToken)
                .Forget();
            mainmenuViewHandler.ExecuteAsync(MainmenuViewEvent.SetStageProgress,
                    (float)index / count)
                .AttachExternalCancellation(ReserveToken)
                .Forget();
            mainmenuViewHandler.ExecuteAsync(MainmenuViewEvent.ShowStageInfo)
                .AttachExternalCancellation(ReserveToken)
                .Forget();
        }

        private async UniTask OnConditionTriggered(IEventTarget e, Condition condition, string value)
        {
            // TODO : test code for testing corner intersection view
            if (condition == Condition.OnSkillStart &&
                e.Owner != Owner)
            {
                IActor actor = (IActor)e;

                // await m_ViewRegistryProvider.StageViewProvider.OpenCornerIntersectionViewAsync(
                //     // portraitImg.Object,
                //     null,
                //     "5252 그렇게 허약해보여서는 이 몸의 공격을 제대로 받아낼 수 있겠어?"
                // );
                // await UniTask.WaitForSeconds(2);
                // await m_ViewRegistryProvider.StageViewProvider.CloseCornerIntersectionViewAsync();
            }
        }

        void IConnector<IUserActorProvider>.Connect(IUserActorProvider    t) => m_UserActorProvider = t;
        void IConnector<IUserActorProvider>.Disconnect(IUserActorProvider t) => m_UserActorProvider = null;

        void IConnector<IStageViewProvider>.      Connect(IStageViewProvider          t) => m_StageViewProvider = t;
        void IConnector<IStageViewProvider>.      Disconnect(IStageViewProvider       t) => m_StageViewProvider = null;
        void IConnector<IActorViewProvider>.Connect(IActorViewProvider    t) => m_ActorViewProvider = t;
        void IConnector<IActorViewProvider>.Disconnect(IActorViewProvider t) => m_ActorViewProvider = null;

        void IConnector<IContentViewEventHandlerProvider>.Connect(IContentViewEventHandlerProvider t) => m_ViewEventHandlerProvider = t;
        void IConnector<IContentViewEventHandlerProvider>.Disconnect(IContentViewEventHandlerProvider t) => m_ViewEventHandlerProvider = null;
    }
}