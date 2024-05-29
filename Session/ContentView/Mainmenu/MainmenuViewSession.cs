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
// File created : 2024, 05, 28 23:05
#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Research;

namespace Vvr.MPC.Session.ContentView.Mainmenu
{
    [UsedImplicitly]
    public sealed class MainmenuViewSession : ContentViewChildSession<MainmenuViewSession.SessionData>,
        IConnector<IMainmenuViewProvider>
    {
        public struct SessionData : ISessionData
        {
            public IContentViewEventHandler<MainmenuViewEvent> eventHandler;
        }

        private IAssetProvider        m_AssetProvider;
        private IMainmenuViewProvider m_MainmenuViewProvider;

        public override string DisplayName => nameof(MainmenuViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            Data.eventHandler.Register(MainmenuViewEvent.OpenResearch, OnOpenResearch);

            Setup().Forget();
        }

        private async UniTask OnOpenResearch(MainmenuViewEvent e, object ctx)
        {
            // TODO: remove parent dependency
            var researchEventHandler = Parent.GetSession<ResearchViewSession>().Data.eventHandler;

            await researchEventHandler.ExecuteAsync(ResearchViewEvent.Open, 0);
        }

        private async UniTaskVoid Setup()
        {
            await UniTask.WaitWhile(() => m_MainmenuViewProvider == null);

            await m_MainmenuViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, null);
        }

        void IConnector<IMainmenuViewProvider>.Connect(IMainmenuViewProvider t)
        {
            m_MainmenuViewProvider = t;
            m_MainmenuViewProvider.Initialize(Data.eventHandler);
        }

        void IConnector<IMainmenuViewProvider>.Disconnect(IMainmenuViewProvider t)
        {
            m_MainmenuViewProvider.Reserve();
            m_MainmenuViewProvider = null;
        }
    }
}