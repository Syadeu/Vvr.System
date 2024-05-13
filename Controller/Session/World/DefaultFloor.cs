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
using Vvr.MPC.Provider;
using Vvr.System.Model;
using Vvr.UI.Observer;

namespace Vvr.System.Controller
{
 	[ParentSession(typeof(DefaultRegion), true)]
    public partial class DefaultFloor : ParentSession<DefaultFloor.SessionData>
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
        private UniTaskCompletionSource m_FloorStartEvent;
        private UniTaskCompletionSource m_StageStartEvent;

        private AsyncLazy<IEventViewProvider> m_ViewProvider;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            m_FloorStartEvent = new();
            m_StageStartEvent = new();

            ObjectObserver<DefaultFloor>.Get(this).EnsureContainer();

            m_ViewProvider = Provider.Static.GetLazyAsync<IEventViewProvider>();
            Provider.Static.Register<IStageProvider>(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_FloorStartEvent.TrySetResult();
            Provider.Static.Unregister<IStageProvider>(this);

            m_ViewProvider = null;

            return base.OnReserve();
        }

        public async UniTask<Result> Start(Owner playerId, ActorSheet.Row[] playerData)
        {
            m_FloorStartEvent.TrySetResult();

            Result              floorResult = default;
            DefaultStage.Result stageResult = default;

            var               startStage  = Data.stages.First;
            List<CachedActor> prevPlayers = new(Data.cachedActors);
            while (startStage != null)
            {
                DefaultStage.SessionData sessionData;
                if (prevPlayers.Count == 0)
                {
                    sessionData = new DefaultStage.SessionData(playerId, startStage.Value,
                        playerData);
                }
                else
                {
                    sessionData = new DefaultStage.SessionData(playerId, startStage.Value,
                        prevPlayers);
                }

                m_CurrentStage = await CreateSession<DefaultStage>(sessionData);
                if (!m_StageStartEvent.TrySetResult())
                {
                    "??".ToLogError();
                }
                {
                    stageResult = await m_CurrentStage.Start();

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

                m_StageStartEvent = new();
                await m_CurrentStage.Reserve();

                "Stage cleared".ToLog();
                await UniTask.Yield();

                startStage = startStage.Next;
            }

            floorResult = new Result(
                prevPlayers.Any() ? prevPlayers.ToArray() : Array.Empty<CachedActor>());

            m_CurrentStage = null;

            return floorResult;
        }
    }
    partial class DefaultFloor : IStageProvider
    {
        public UniTask FloorStartTask => m_FloorStartEvent.Task;
        public UniTask StageStartTask => m_StageStartEvent.Task;

        public DefaultStage       CurrentStage   => m_CurrentStage;
        public IReadOnlyActorList Timeline       => CurrentStage.Timeline;
        public IReadOnlyActorList HandActors     => CurrentStage.HandActors;
        public IReadOnlyActorList PlayerField    => CurrentStage.PlayerField;
        public IReadOnlyActorList EnemyField     => CurrentStage.EnemyField;
    }
}