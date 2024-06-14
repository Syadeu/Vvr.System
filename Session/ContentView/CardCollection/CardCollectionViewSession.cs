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
// File created : 2024, 06, 04 23:06
#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.Provider;

namespace Vvr.Session.ContentView.CardCollection
{
    [UsedImplicitly]
    public sealed class CardCollectionViewSession
        : ContentViewChildSession<CardCollectionViewEvent, ICardCollectionViewProvider>,
            IConnector<IUserActorProvider>
    {
        private IAssetProvider     m_AssetProvider;
        private IUserActorProvider m_UserActorProvider;

        public override string DisplayName => nameof(CardCollectionViewSession);

        private GameObject m_ViewInstance;

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);
            Register(m_AssetProvider);

            EventHandler
                .Register(CardCollectionViewEvent.Open, OnOpen)
                .Register(CardCollectionViewEvent.Close, OnClose)
                ;
        }

        private async UniTask OnOpen(CardCollectionViewEvent e, object ctx)
        {
            IResolvedActorData data = ctx as IResolvedActorData;

            CardCollectionViewOpenContext context = new CardCollectionViewOpenContext()
            {
                selected = data,
                data     = m_UserActorProvider.PlayerActors
            };

            m_ViewInstance = await ViewProvider
                    .OpenAsync(CanvasViewProvider, m_AssetProvider, context)
                    .AttachExternalCancellation(ReserveToken)
                ;
            this.Inject(m_ViewInstance);
        }
        private async UniTask OnClose(CardCollectionViewEvent e, object ctx)
        {
            if (m_ViewInstance is not null)
            {
                this.Detach(m_ViewInstance);
            }

            m_ViewInstance = null;

            await ViewProvider
                    .CloseAsync(ctx)
                    .AttachExternalCancellation(ReserveToken)
                ;
        }

        void IConnector<IUserActorProvider>.Connect(IUserActorProvider    t) => m_UserActorProvider = t;
        void IConnector<IUserActorProvider>. Disconnect(IUserActorProvider t) => m_UserActorProvider = null;
    }
}