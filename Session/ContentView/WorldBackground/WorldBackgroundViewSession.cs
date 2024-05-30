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
    public class WorldBackgroundViewSession : ContentViewChildSession<WorldBackgroundViewSession.SessionData>,
        IConnector<IWorldBackgroundViewProvider>
    {
        public struct SessionData : ISessionData
        {
            public IContentViewEventHandler<WorldBackgroundViewEvent> eventHandler;
        }

        private IAssetProvider               m_AssetProvider;
        private IWorldBackgroundViewProvider m_ViewProvider;

        public override string DisplayName => nameof(WorldBackgroundViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            data.eventHandler.Register(WorldBackgroundViewEvent.Open, OnOpen);
            data.eventHandler.Register(WorldBackgroundViewEvent.Close, OnClose);
        }

        private async UniTask OnOpen(WorldBackgroundViewEvent e, object ctx)
        {
            await m_ViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, ctx);
        }
        private async UniTask OnClose(WorldBackgroundViewEvent e, object ctx)
        {
            await m_ViewProvider.CloseAsync(ctx);
        }

        // public async UniTask OpenAsync(Sprite sprite)
        // {
        //     await m_ViewProvider.OpenAsync(m_AssetProvider, sprite);
        //
        //     await m_ViewProvider.View.SetBackgroundAsync(sprite);
        // }

        void IConnector<IWorldBackgroundViewProvider>.Connect(IWorldBackgroundViewProvider t)
        {
            m_ViewProvider = t;

            m_ViewProvider.Initialize(Data.eventHandler);
        }
        void IConnector<IWorldBackgroundViewProvider>.Disconnect(IWorldBackgroundViewProvider t)
        {
            m_ViewProvider.Reserve();
            m_ViewProvider = null;
        }
    }
}