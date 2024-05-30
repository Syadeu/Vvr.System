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
// File created : 2024, 05, 27 16:05
#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.BattleSign
{
    [UsedImplicitly]
    public sealed class BattleSignViewSession : ParentSession<BattleSignViewSession.SessionData>,
        IConnector<IBattleSignViewProvider>
    {
        public struct SessionData : ISessionData
        {
            public IContentViewEventHandler<BattleSignViewEvent> eventHandler;
        }

        private IAssetProvider          m_AssetProvider;
        private IBattleSignViewProvider m_BattleSignViewProvider;

        public override string DisplayName => nameof(BattleSignViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);


        }
        protected override UniTask OnReserve()
        {
            return base.OnReserve();
        }

        void IConnector<IBattleSignViewProvider>.Connect(IBattleSignViewProvider t)
        {
            m_BattleSignViewProvider = t;

            m_BattleSignViewProvider.Initialize(Data.eventHandler);
        }
        void IConnector<IBattleSignViewProvider>.Disconnect(IBattleSignViewProvider t)
        {
            m_BattleSignViewProvider.Reserve();
            m_BattleSignViewProvider = null;
        }
    }
}