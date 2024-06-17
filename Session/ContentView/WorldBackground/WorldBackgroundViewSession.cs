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
// File created : 2024, 05, 27 10:05
#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.WorldBackground
{
    /// <summary>
    /// Represents a session for the world background view.
    /// </summary>
    [UsedImplicitly]
    public class WorldBackgroundViewSession : ContentViewChildSession<WorldBackgroundViewEvent>,
        IConnector<IWorldBackgroundViewProvider>
    {
        private IAssetProvider               m_AssetProvider;
        private IWorldBackgroundViewProvider m_ViewProvider;

        public override string DisplayName => nameof(WorldBackgroundViewSession);

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);
            Register(m_AssetProvider);

            EventHandler
                .Register(WorldBackgroundViewEvent.Open, OnOpen)
                .Register(WorldBackgroundViewEvent.Close, OnClose);
        }

        private UniTask OnOpen(WorldBackgroundViewEvent e, object ctx)
        {
            return m_ViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, ctx, ReserveToken);
        }
        private UniTask OnClose(WorldBackgroundViewEvent e, object ctx)
        {
            return m_ViewProvider.CloseAsync(ctx, ReserveToken);
        }

        void IConnector<IWorldBackgroundViewProvider>.Connect(IWorldBackgroundViewProvider    t) => m_ViewProvider = t;
        void IConnector<IWorldBackgroundViewProvider>.Disconnect(IWorldBackgroundViewProvider t) => m_ViewProvider = null;
    }
}