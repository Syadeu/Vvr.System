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
// File created : 2024, 05, 23 19:05
#endregion

using System.Linq;
using Cysharp.Threading.Tasks;
using Vvr.Controller.Research;
using Vvr.Provider;
using Vvr.Provider.ContentView;
using Vvr.Session.Provider;

namespace Vvr.Session.View
{
    public sealed class ResearchViewSession : ParentSession<ResearchViewSession.SessionData>,
        IConnector<IResearchDataProvider>,
        IConnector<IResearchViewProvider>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(ResearchViewSession);

        private AssetSession m_AssetSession;

        private IResearchDataProvider m_ResearchDataProvider;
        private IResearchViewProvider m_ResearchViewProvider;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetSession = await CreateSession<AssetSession>(default);
        }

        protected override async UniTask OnReserve()
        {
            m_AssetSession = null;

            await base.OnReserve();
        }

        void IConnector<IResearchDataProvider>.Connect(IResearchDataProvider    t) => m_ResearchDataProvider = t;
        void IConnector<IResearchDataProvider>.Disconnect(IResearchDataProvider t) => m_ResearchDataProvider = null;

        void IConnector<IResearchViewProvider>.Connect(IResearchViewProvider    t) => m_ResearchViewProvider = t;
        void IConnector<IResearchViewProvider>.Disconnect(IResearchViewProvider t) => m_ResearchViewProvider = null;
    }
}