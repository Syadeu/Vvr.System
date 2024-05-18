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
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Input;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
    [ParentSession(typeof(DefaultMap))]
    public class DefaultRegion : ParentSession<DefaultRegion.SessionData>,
        IConnector<IPlayerActorProvider>,
        IConnector<IManualInputProvider>,

    // TODO: Temp
        IConnector<IStageDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        private IPlayerActorProvider m_PlayerActorProvider;
        private IStageDataProvider   m_StageProvider;

        private bool m_IsAutoControl;

        public override string DisplayName => nameof(DefaultRegion);

        private UniTask<IChildSession> CurrentControlSession { get; set; }

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            CurrentControlSession = SwitchControl(null);

            var actorProvider = await CreateSession<ActorFactorySession>(default);
            Register<IActorProvider>(actorProvider);

            Vvr.Provider.Provider.Static.Connect<IManualInputProvider>(this);

            await base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IManualInputProvider>(this);

            return base.OnReserve();
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

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider t) => m_StageProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageProvider = null;

        void IConnector<IManualInputProvider>.Connect(IManualInputProvider t)
        {
            m_IsAutoControl = false;
            CurrentControlSession = CurrentControlSession.ContinueWith(SwitchControl);
        }

        void IConnector<IManualInputProvider>.Disconnect(IManualInputProvider t)
        {
            m_IsAutoControl     = true;
            CurrentControlSession = CurrentControlSession.ContinueWith(SwitchControl);
        }

        private async UniTask<IChildSession> SwitchControl(IChildSession existing)
        {
            if (ReserveToken.IsCancellationRequested) return null;

            if (existing is not null) await existing.Reserve();

            if (m_IsAutoControl)
            {
                return await CreateSession<AIControlSession>(default);
            }
            return await CreateSession<PlayerControlSession>(default);
        }
    }
}