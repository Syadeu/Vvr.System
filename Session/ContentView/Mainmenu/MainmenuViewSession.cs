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

namespace Vvr.MPC.Session.ContentView.Mainmenu
{
    [UsedImplicitly]
    public sealed class MainmenuViewSession : ParentSession<MainmenuViewSession.SessionData>,
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
        }

        public void Connect(IMainmenuViewProvider t)
        {
            m_MainmenuViewProvider = t;
            m_MainmenuViewProvider.Initialize(Data.eventHandler);
        }

        public void Disconnect(IMainmenuViewProvider t)
        {
            m_MainmenuViewProvider.Reserve();
            m_MainmenuViewProvider = null;
        }
    }
}