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

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Research;
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session.ContentView.Research
{
    [UsedImplicitly]
    public sealed class ResearchViewSession : ParentSession<ResearchViewSession.SessionData>,
        IConnector<IUserDataProvider>,
        IConnector<IResearchDataProvider>,
        IConnector<IResearchViewProvider>
    {
        public struct SessionData : ISessionData
        {
            public IContentViewEventHandler<ResearchViewEvent> eventHandler;
        }

        public override string DisplayName => nameof(ResearchViewSession);

        private AssetSession m_AssetSession;

        private IUserDataProvider     m_UserDataProvider;
        private IResearchDataProvider m_ResearchDataProvider;
        private IResearchViewProvider m_ResearchViewProvider;

        private bool m_Opened = false;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            data.eventHandler.Register(ResearchViewEvent.Open, OnOpen);
            data.eventHandler.Register(ResearchViewEvent.Close, OnClose);
            data.eventHandler.Register(ResearchViewEvent.SelectGroupWithIndex, OnSelectGroupWithIndex);
            data.eventHandler.Register(ResearchViewEvent.Upgrade, OnUpgrade);
        }

        private async UniTask OnOpen(ResearchViewEvent e, object ctx)
        {
            if (m_Opened)
            {
                // Is in opening sequence or already opened
                return;
            }

            m_Opened = true;

            m_AssetSession = await CreateSession<AssetSession>(default);
            foreach (var nodeGroup in m_ResearchDataProvider)
            {
                nodeGroup.RegisterAssetProvider(m_AssetSession);
            }

            await m_ResearchViewProvider.Open(m_AssetSession, ctx);
        }
        private async UniTask OnClose(ResearchViewEvent e, object ctx)
        {
            if (!m_Opened)
                return;

            await m_ResearchViewProvider.Close(ctx);

            foreach (var nodeGroup in m_ResearchDataProvider)
            {
                nodeGroup.UnregisterAssetProvider();
            }
            await m_AssetSession.Reserve();

            m_AssetSession = null;
            m_Opened       = false;
        }

        protected override async UniTask OnReserve()
        {
            Data.eventHandler.Dispose();

            Data.eventHandler.Unregister(ResearchViewEvent.SelectGroupWithIndex, OnSelectGroupWithIndex);

            m_AssetSession             = null;

            await base.OnReserve();
        }

        private async UniTask OnUpgrade(ResearchViewEvent e, object ctx)
        {
            IResearchNode node = (IResearchNode)ctx;

            if (node.MaxLevel <= node.Level)
            {
                "already max level".ToLogError();
                return;
            }

            int lvl = m_UserDataProvider.GetInt(UserDataKeyCollection.Research.NodeLevel(node.Id));
            if (lvl != node.Level)
                throw new InvalidOperationException("lvl has been modified");

            $"Upgrade node {node.Id}".ToLog();

            lvl += 1;
            m_UserDataProvider.SetInt(UserDataKeyCollection.Research.NodeLevel(node.Id), lvl);
            node.SetLevel(lvl);

            Data.eventHandler.Execute(ResearchViewEvent.Update, node)
                .Forget();
        }

        private async UniTask OnSelectGroupWithIndex(ResearchViewEvent e, object ctx)
        {
            int index = (int)ctx;

            IResearchNodeGroup group = m_ResearchDataProvider[index];
            await Data.eventHandler.Execute(ResearchViewEvent.SelectGroup, group);

            // TODO: select last upgraded node
            await Data.eventHandler.Execute(ResearchViewEvent.Select, group.Root);
        }

        void IConnector<IResearchDataProvider>.Connect(IResearchDataProvider    t) => m_ResearchDataProvider = t;
        void IConnector<IResearchDataProvider>.Disconnect(IResearchDataProvider t) => m_ResearchDataProvider = null;

        void IConnector<IResearchViewProvider>.Connect(IResearchViewProvider    t)
        {
            m_ResearchViewProvider = t;

            m_ResearchViewProvider.Initialize(Data.eventHandler);
        }

        void IConnector<IResearchViewProvider>.Disconnect(IResearchViewProvider t) => m_ResearchViewProvider = null;
        void IConnector<IUserDataProvider>.    Connect(IUserDataProvider        t) => m_UserDataProvider = t;
        void IConnector<IUserDataProvider>.    Disconnect(IUserDataProvider     t) => m_UserDataProvider = null;
    }
}