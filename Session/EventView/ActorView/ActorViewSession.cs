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
// File created : 2024, 06, 20 17:06

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.AssetManagement;

namespace Vvr.Session.EventView.ActorView
{
    [UsedImplicitly]
    public sealed class ActorViewSession : ParentSession<ActorViewSession.SessionData>,
        IConnector<IActorViewProvider>,
        IConnector<ICanvasViewProvider>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(ActorViewSession);

        private IAssetProvider      m_AssetProvider;
        private IActorViewProvider  m_ActorViewProvider;
        private ICanvasViewProvider m_CanvasViewProvider;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            await m_ActorViewProvider.OpenAsync(m_CanvasViewProvider, m_AssetProvider, ReserveToken);
        }

        protected override async UniTask OnReserve()
        {
            await m_ActorViewProvider.CloseAsync();

            await base.OnReserve();
        }

        void IConnector<IActorViewProvider>.Connect(IActorViewProvider t) => m_ActorViewProvider = t;
        void IConnector<IActorViewProvider>.Disconnect(IActorViewProvider t) => m_ActorViewProvider = null;

        void IConnector<ICanvasViewProvider>.Connect(ICanvasViewProvider    t) => m_CanvasViewProvider = t;
        void IConnector<ICanvasViewProvider>.Disconnect(ICanvasViewProvider t) => m_CanvasViewProvider = null;
    }
}