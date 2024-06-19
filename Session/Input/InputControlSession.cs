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

using System.Threading;
using Cysharp.Threading.Tasks;
using Vvr.Provider;

namespace Vvr.Session.Input
{
    public abstract class InputControlSession<TSessionData>
        : ChildSession<TSessionData>,
        IInputControlProvider

        where TSessionData : ISessionData
    {
        private IEventTarget m_InputOwner;

        private CancellationTokenSource m_CancellationTokenSource;

        protected override UniTask OnInitialize(IParentSession session, TSessionData data)
        {
            Parent.Register<IInputControlProvider>(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Parent.Unregister<IInputControlProvider>();

            return base.OnReserve();
        }

        public abstract bool CanControl(IEventTarget target);

        async UniTask IInputControlProvider.TransferControl(IEventTarget target, CancellationToken cancellationToken)
        {
            using var sts = CancellationTokenSource.CreateLinkedTokenSource(ReserveToken, cancellationToken);

            await OnControl(target, sts.Token)
                .AttachExternalCancellation(ReserveToken);

            if (sts.Token.IsCancellationRequested)
                "Canceled input control".ToLog();
        }

        protected abstract UniTask OnControl(IEventTarget target, CancellationToken cancellationToken);
    }
}