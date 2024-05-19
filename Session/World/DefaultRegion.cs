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
using JetBrains.Annotations;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Input;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
    [UsedImplicitly, ParentSession(typeof(DefaultMap))]
    public class DefaultRegion : ParentSession<DefaultRegion.SessionData>,
        IConnector<IUserActorProvider>,
        IConnector<IUserStageProvider>,
        IConnector<IManualInputProvider>,

    // TODO: Temp
        IConnector<IStageDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        private IUserActorProvider m_UserActorProvider;
        private IUserStageProvider m_UserStageProvider;
        private IStageDataProvider m_StageDataProvider;

        private bool m_IsAutoControl;

        public override string DisplayName => nameof(DefaultRegion);

        private UniTask<IChildSession> CurrentControlSession { get; set; }

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            CurrentControlSession = SwitchControl(null);

            var actorProvider = await CreateSession<ActorFactorySession>(default);
            Register<IActorProvider>(actorProvider);

            Vvr.Provider.Provider.Static.Connect<IManualInputProvider>(this);

            await Start();
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IManualInputProvider>(this);

            return base.OnReserve();
        }

        private async UniTask Start()
        {
            using var trigger = ConditionTrigger.Push(this, DisplayName);
            // StageSheet.Row startStage = Data.sheet[Data.startStageId];
            // var startStage = m_StageDataProvider.First().Value;
            var startStage = m_UserStageProvider.CurrentStage;

            var             currentStage = startStage;
            LinkedList<IStageData> list         = new();

            IStageActor[] aliveActors = Array.Empty<IStageActor>();
            do
            {
                list.AddLast(currentStage);
                if (currentStage.IsFinalStage)
                {
                    var floor = await CreateSession<DefaultFloor>(new DefaultFloor.SessionData(list, aliveActors));

                    DefaultFloor.Result result = await floor.Start(m_UserActorProvider.GetCurrentTeam());
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

        void IConnector<IUserActorProvider>.Connect(IUserActorProvider    t) => m_UserActorProvider = t;
        void IConnector<IUserActorProvider>.Disconnect(IUserActorProvider t) => m_UserActorProvider = null;

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider t) => m_StageDataProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageDataProvider = null;

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

        void IConnector<IUserStageProvider>.Connect(IUserStageProvider    t) => m_UserStageProvider = t;
        void IConnector<IUserStageProvider>.Disconnect(IUserStageProvider t) => m_UserStageProvider = null;
    }
}