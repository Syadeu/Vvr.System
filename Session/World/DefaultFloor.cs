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
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
 	// [ParentSession(typeof(DefaultRegion), true)]
    public partial class DefaultFloor : ParentSession<DefaultFloor.SessionData>,
        IConnector<IViewRegistryProvider>
    {
        public struct SessionData : ISessionData
        {
            public readonly IEnumerable<IStageData> stages;
            public readonly IStageActor[]              existingActors;

            public SessionData(IEnumerable<IStageData> s, IStageActor[] existingActors)
            {
                stages              = s;
                this.existingActors = existingActors;
            }
        }

        public struct Result
        {
            public readonly IStageActor[] alivePlayerActors;

            public Result(IStageActor[] x)
            {
                alivePlayerActors = x;
            }
        }

        private DefaultStage          m_CurrentStage;
        private IViewRegistryProvider m_ViewRegistryProvider;

        public override string DisplayName => nameof(DefaultFloor);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            // We dont need to manually close these sessions
            // When this session close, child session also closed.
            var timelineSession = await CreateSession<TimelineQueueSession>(default);
            var assetSession    = await CreateSession<AssetSession>(default);
            var stageActorCreateSession = await CreateSession<StageActorFactorySession>(default);

            // Register providers to inject child sessions.
            Register<ITimelineQueueProvider>(timelineSession)
                .Register<IAssetProvider>(assetSession)
                .Register<IStageActorProvider>(stageActorCreateSession);

            await base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            return base.OnReserve();
        }

        public async UniTask<Result> Start(IEnumerable<IActorData> playerData)
        {
            using var trigger = ConditionTrigger.Push(this, DisplayName);

            Started = true;

            Result              floorResult = default;
            DefaultStage.Result stageResult = default;

            // LinkedListNode<StageSheet.Row> startStage  = Data.stages.First;
            List<IStageActor>              prevPlayers = new(Data.existingActors);

            string cachedStartStageId = Data.stages.First().Id;
            await trigger.Execute(Model.Condition.OnFloorStarted, cachedStartStageId);

            foreach (IStageData stage in Data.stages)
            {
                DefaultStage.SessionData sessionData;
                if (prevPlayers.Count == 0)
                {
                    sessionData = new DefaultStage.SessionData(stage,
                        playerData);
                }
                else
                {
                    sessionData = new DefaultStage.SessionData(stage,
                        prevPlayers);
                }

                m_CurrentStage = await CreateSession<DefaultStage>(sessionData);
                Parent.Register<IStageInfoProvider>(m_CurrentStage);
                {
                    await trigger.Execute(Model.Condition.OnStageStarted, sessionData.stage.Id);
                    stageResult = await m_CurrentStage.Start();
                    await trigger.Execute(Model.Condition.OnStageEnded, sessionData.stage.Id);

                    foreach (var enemy in stageResult.enemyActors)
                    {
                        m_ViewRegistryProvider.CardViewProvider.Release(enemy.Owner);
                        enemy.Owner.Release();
                    }

                    if (!stageResult.playerActors.Any())
                    {
                        "all players dead".ToLog();
                        break;
                    }

                    prevPlayers.Clear();
                    prevPlayers.AddRange(stageResult.playerActors);
                }
                Parent.Unregister<IStageInfoProvider>();
                await m_CurrentStage.Reserve();

                "Stage cleared".ToLog();
                await UniTask.Yield();
            }

            floorResult = new Result(
                prevPlayers.Any() ? prevPlayers.ToArray() : Array.Empty<IStageActor>());

            m_CurrentStage = null;
            Started        = false;

            await trigger.Execute(Model.Condition.OnFloorEnded, cachedStartStageId);

            return floorResult;
        }

        protected override UniTask OnCreateSession(IChildSession session)
        {
            Assert.IsNotNull(m_ViewRegistryProvider);

            session.Register<IEventViewProvider>(m_ViewRegistryProvider.CardViewProvider);

            return base.OnCreateSession(session);
        }

        public void Connect(IViewRegistryProvider    t) => m_ViewRegistryProvider = t;
        public void Disconnect(IViewRegistryProvider t) => m_ViewRegistryProvider = null;
    }
}