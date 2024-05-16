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
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
 	[ParentSession(typeof(DefaultRegion), true)]
    public partial class DefaultFloor : ParentSession<DefaultFloor.SessionData>,
        IConnector<IActorProvider>
    {
        public struct SessionData : ISessionData
        {
            public readonly LinkedList<StageSheet.Row> stages;
            public readonly CachedActor[]              cachedActors;

            public SessionData(LinkedList<StageSheet.Row> s, CachedActor[] actors)
            {
                stages       = s;
                cachedActors = actors;
            }
        }

        public struct Result
        {
            public readonly CachedActor[] alivePlayerActors;

            public Result(CachedActor[] x)
            {
                alivePlayerActors = x;
            }
        }

        private DefaultStage            m_CurrentStage;

        private AsyncLazy<IEventViewProvider> m_ViewProvider;

        public override string DisplayName => nameof(DefaultFloor);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            m_ViewProvider = Vvr.Provider.Provider.Static.GetLazyAsync<IEventViewProvider>();

            // We dont need to manually close these sessions
            // When this session close, child session also closed.
            var timelineSession = await CreateSession<TimelineQueueSession>(default);
            var assetSession    = await CreateSession<AssetSession>(default);
            var stageActorCreateSession = await CreateSession<StageActorCreateSession>(default);

            // Register providers to inject child sessions.
            Register<ITimelineQueueProvider>(timelineSession)
                .Register<IAssetProvider>(assetSession)
                .Register<IStageActorProvider>(stageActorCreateSession);

            await base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_ViewProvider = null;

            return base.OnReserve();
        }

        public async UniTask<Result> Start(IEnumerable<IActorData> playerData)
        {
            using var trigger = ConditionTrigger.Push(this, DisplayName);

            Started = true;

            Result              floorResult = default;
            DefaultStage.Result stageResult = default;

            LinkedListNode<StageSheet.Row> startStage  = Data.stages.First;
            List<CachedActor>              prevPlayers = new(Data.cachedActors);

            string cachedStartStageId = startStage.Value.Id;
            await trigger.Execute(Model.Condition.OnFloorStarted, cachedStartStageId);

            while (startStage != null)
            {
                DefaultStage.SessionData sessionData;
                if (prevPlayers.Count == 0)
                {
                    sessionData = new DefaultStage.SessionData(startStage.Value,
                        playerData);
                }
                else
                {
                    sessionData = new DefaultStage.SessionData(startStage.Value,
                        prevPlayers);
                }

                m_CurrentStage = await CreateSession<DefaultStage>(sessionData);
                Parent.Register<IStageProvider>(m_CurrentStage);
                {
                    await trigger.Execute(Model.Condition.OnStageStarted, sessionData.stage.Id);
                    stageResult = await m_CurrentStage.Start();
                    await trigger.Execute(Model.Condition.OnStageEnded, sessionData.stage.Id);

                    var viewProvider = await m_ViewProvider;
                    foreach (var enemy in stageResult.enemyActors)
                    {
                        viewProvider.Release(enemy.Owner);
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
                Parent.Unregister<IStageProvider>();
                await m_CurrentStage.Reserve();

                "Stage cleared".ToLog();
                await UniTask.Yield();

                startStage = startStage.Next;
            }

            floorResult = new Result(
                prevPlayers.Any() ? prevPlayers.ToArray() : Array.Empty<CachedActor>());

            m_CurrentStage = null;
            Started        = false;

            await trigger.Execute(Model.Condition.OnFloorEnded, cachedStartStageId);

            return floorResult;
        }

        void IConnector<IActorProvider>.Connect(IActorProvider t)
        {
        }
        void IConnector<IActorProvider>.Disconnect(IActorProvider t)
        {
        }
    }
}