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
// File created : 2024, 06, 02 20:06

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.Provider;

namespace Vvr.Session.ContentView.Deck
{
    [UsedImplicitly]
    public sealed class DeckViewSession : ContentViewChildSession<DeckViewEvent, IDeckViewProvider>,
        IConnector<IUserActorProvider>
    {
        private IAssetProvider     m_AssetProvider;
        private IUserActorProvider m_UserActorProvider;

        private GameObject m_ViewInstance;

        public override string DisplayName => nameof(DeckViewSession);

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);
            Register(m_AssetProvider);

            EventHandler
                .Register(DeckViewEvent.Open, OnOpen)
                .Register(DeckViewEvent.Close, OnClose)

                .Register(DeckViewEvent.CardSelect, OnCardSelect)
                ;
        }
        protected override UniTask OnReserve()
        {
            if (m_ViewInstance is not null)
                this.Detach(m_ViewInstance);

            EventHandler
                .Unregister(DeckViewEvent.Open, OnOpen)
                .Unregister(DeckViewEvent.Close, OnClose)

                .Unregister(DeckViewEvent.CardSelect, OnCardSelect)
                ;

            m_ViewInstance = null;

            return base.OnReserve();
        }

        private async UniTask OnCardSelect(DeckViewEvent e, object ctx)
        {
            if (ctx is not int idx)
            {
                "invalid ctx".ToLogError();
                return;
            }

            IResolvedActorData actor = m_UserActorProvider.GetCurrentTeam()[idx];
            await EventHandlerProvider.Resolve<CardCollectionViewEvent>()
                .ExecuteAsync(CardCollectionViewEvent.Open, actor)
                .AttachExternalCancellation(ReserveToken)
                ;
        }

        private async UniTask OnOpen(DeckViewEvent e, object ctx)
        {
            if (ctx is not DeckViewOpenContext openContext)
            {
                var currentTeam = m_UserActorProvider.GetCurrentTeam();

                DeckViewSetActorContext[] actorContexts = new DeckViewSetActorContext[currentTeam.Count];

                int i = 0;
                foreach (var actor in currentTeam)
                {
                    if (actor is null)
                    {
                        actorContexts[i] = new DeckViewSetActorContext()
                        {
                            index = i++
                        };
                        continue;
                    }

                    var portraitAssetPath = actor.Assets[AssetType.ContextPortrait];
                    var    portraitImg  = await m_AssetProvider
                        .LoadAsync<Sprite>(portraitAssetPath);

                    actorContexts[i] = new DeckViewSetActorContext()
                    {
                        index    = i++,
                        id = actor.Id,

                        portrait = portraitImg.Object,
                        title    = actor.Id,
                        grade    = actor.Grade,
                        // TODO: actor level
                        level = 0
                    };
                }

                openContext = new DeckViewOpenContext()
                {
                    actorContexts = actorContexts,
                };
            }

            m_ViewInstance = await ViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, ctx);
            this.Inject(m_ViewInstance);

            foreach (var actorContext in openContext.actorContexts)
            {
                await EventHandler.ExecuteAsync(DeckViewEvent.SetActor, actorContext);
            }
        }
        private async UniTask OnClose(DeckViewEvent e, object ctx)
        {
            this.Detach(m_ViewInstance);
            await ViewProvider.CloseAsync(ctx);

            m_ViewInstance = null;
        }

        void IConnector<IUserActorProvider>.Connect(IUserActorProvider    t) => m_UserActorProvider = t;
        void IConnector<IUserActorProvider>. Disconnect(IUserActorProvider t) => m_UserActorProvider = null;
    }
}