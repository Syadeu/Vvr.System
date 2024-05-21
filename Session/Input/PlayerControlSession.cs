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
// File created : 2024, 05, 16 23:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Provider;

namespace Vvr.Session.Input
{
    [UsedImplicitly]
    public sealed class PlayerControlSession : AIControlSession,
        IConnector<IManualInputProvider>
    {
        private IManualInputProvider m_ManualInputProvider;

        public override string DisplayName => nameof(PlayerControlSession);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Vvr.Provider.Provider.Static.Connect<IManualInputProvider>(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IManualInputProvider>(this);

            return base.OnReserve();
        }

        public override bool CanControl(IEventTarget target)
        {
            return target is IActor;
        }

        protected override async UniTask OnControl(IEventTarget  target)
        {
            // TODO: testing
            if (Owner != target.Owner)
            {
                // AI
                await base.OnControl(target);
                return;
            }

            if (m_ManualInputProvider == null)
            {
                "[Input] No manual input provider found".ToLog();
                await base.OnControl(target);
            }
            else
            {
                IActor actor     = (IActor)target;
                var    actorData = ActorDataProvider.Resolve(actor.Id);

                await m_ManualInputProvider.OnControl(actor, actorData);
            }
        }

        void IConnector<IManualInputProvider>.Connect(IManualInputProvider    t) => m_ManualInputProvider = t;
        void IConnector<IManualInputProvider>.Disconnect(IManualInputProvider t) => m_ManualInputProvider = null;
    }
}