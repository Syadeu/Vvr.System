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
    [ParentSession(typeof(DefaultMap))]
    public class DefaultRegion : ParentSession<DefaultRegion.SessionData>,
        IConnector<IPlayerActorProvider>,
        // TODO: Temp
        IConnector<IStageDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        private IPlayerActorProvider m_PlayerActorProvider;

        public override string DisplayName => nameof(DefaultRegion);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await CreateSession<PlayerControlSession>(default);

            await base.OnInitialize(session, data);
        }

        public async UniTask Start()
        {
            using var trigger = ConditionTrigger.Push(this, DisplayName);
            // StageSheet.Row startStage = Data.sheet[Data.startStageId];
            var startStage = m_StageProvider.First().Value;

            var             currentStage = startStage;
            LinkedList<IStageData> list         = new();

            IStageActor[] aliveActors = Array.Empty<IStageActor>();
            do
            {
                list.AddLast(currentStage);
                if (currentStage.IsFinalStage)
                {
                    var floor = await CreateSession<DefaultFloor>(new DefaultFloor.SessionData(list, aliveActors));

                    DefaultFloor.Result result = await floor.Start(m_PlayerActorProvider.GetCurrentTeam());
                    aliveActors = result.alivePlayerActors;

                    await floor.Reserve();

                    "Floor cleared".ToLog();
                    list.Clear();

                    if (aliveActors.Length <= 0)
                    {
                        "All player is dead reset".ToLog();
                        currentStage = startStage;
                    }
                }

                currentStage = currentStage.NextStage;
                await UniTask.Yield();
            } while (currentStage != null);
        }

        void IConnector<IPlayerActorProvider>.Connect(IPlayerActorProvider    t) => m_PlayerActorProvider = t;
        void IConnector<IPlayerActorProvider>.Disconnect(IPlayerActorProvider t) => m_PlayerActorProvider = null;

        private IStageDataProvider m_StageProvider;

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider t)
        {
            m_StageProvider = t;
        }

        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t)
        {
            m_StageProvider = null;
        }
    }
}