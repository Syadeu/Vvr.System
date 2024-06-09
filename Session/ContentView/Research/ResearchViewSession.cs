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
using UnityEngine;
using Vvr.Controller.Research;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.Provider;

namespace Vvr.Session.ContentView.Research
{
    /// <summary>
    /// Represents a research view session.
    /// </summary>
    [UsedImplicitly]
    public sealed class ResearchViewSession : ContentViewChildSession<ResearchViewEvent, IResearchViewProvider>,
        IConnector<IUserDataProvider>,
        IConnector<IResearchDataProvider>
    {
        public override string DisplayName => nameof(ResearchViewSession);

        private AssetSession m_AssetSession;

        private IUserDataProvider     m_UserDataProvider;
        private IResearchDataProvider m_ResearchDataProvider;

        private GameObject m_ViewInstance;

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            EventHandler
                .Register(ResearchViewEvent.Open, OnOpen)
                .Register(ResearchViewEvent.Close, OnClose)
                .Register(ResearchViewEvent.SelectGroupWithIndex, OnSelectGroupWithIndex)
                .Register(ResearchViewEvent.Upgrade, OnUpgrade);
        }
        protected override async UniTask OnReserve()
        {
            if (m_ViewInstance is not null)
                this.Detach(m_ViewInstance);

            m_AssetSession = null;
            m_ViewInstance = null;

            await base.OnReserve();
        }

        private async UniTask OnOpen(ResearchViewEvent e, object ctx)
        {
            if (m_ViewInstance is not null)
            {
                // Is in opening sequence or already opened
                return;
            }

            m_AssetSession = await CreateSession<AssetSession>(default);
            Register<IAssetProvider>(m_AssetSession);
            foreach (var nodeGroup in m_ResearchDataProvider)
            {
                nodeGroup.RegisterAssetProvider(m_AssetSession);
            }

            m_ViewInstance = await ViewProvider.OpenAsync(
                CanvasViewProvider,
                m_AssetSession, ctx);
            this.Inject(m_ViewInstance);

            if (ctx is int groupIndex)
            {
                await EventHandler
                    .ExecuteAsync(ResearchViewEvent.SelectGroupWithIndex, groupIndex)
                    .AttachExternalCancellation(ReserveToken)
                    .SuppressCancellationThrow()
                    ;
            }
            else
            {
                await EventHandler
                    .ExecuteAsync(ResearchViewEvent.SelectGroupWithIndex, 0)
                    .AttachExternalCancellation(ReserveToken)
                    .SuppressCancellationThrow()
                    ;
            }
        }
        private async UniTask OnClose(ResearchViewEvent e, object ctx)
        {
            if (m_ViewInstance is null)
                return;

            this.Detach(m_ViewInstance);
            await ViewProvider.CloseAsync(ctx);

            foreach (var nodeGroup in m_ResearchDataProvider)
            {
                nodeGroup.UnregisterAssetProvider();
            }

            Unregister<IAssetProvider>();
            await m_AssetSession.Reserve();

            m_AssetSession = null;
            m_ViewInstance = null;
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

            EventHandler
                .ExecuteAsync(ResearchViewEvent.Update, node)
                .AttachExternalCancellation(ReserveToken)
                .SuppressCancellationThrow()
                .Forget();
        }

        private async UniTask OnSelectGroupWithIndex(ResearchViewEvent e, object ctx)
        {
            int index = (int)ctx;

            IResearchNodeGroup group = m_ResearchDataProvider[index];
            await EventHandler.ExecuteAsync(ResearchViewEvent.SelectGroup, group);

            // TODO: select last upgraded node
            await EventHandler.ExecuteAsync(ResearchViewEvent.Select, group.Root);
        }

        void IConnector<IResearchDataProvider>.Connect(IResearchDataProvider    t) => m_ResearchDataProvider = t;
        void IConnector<IResearchDataProvider>.Disconnect(IResearchDataProvider t) => m_ResearchDataProvider = null;

        void IConnector<IUserDataProvider>.    Connect(IUserDataProvider        t) => m_UserDataProvider = t;
        void IConnector<IUserDataProvider>.    Disconnect(IUserDataProvider     t) => m_UserDataProvider = null;
    }
}