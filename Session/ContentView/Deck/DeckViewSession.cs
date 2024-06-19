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

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Provider.Command;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.Provider;
using Vvr.Session.World.Core;

namespace Vvr.Session.ContentView.Deck
{
    [UsedImplicitly]
    public sealed class DeckViewSession : ContentViewChildSession<DeckViewEvent, IDeckViewProvider>,
        IConnector<IUserActorProvider>,
        IConnector<ICommandProvider>
    {
        private IAssetProvider     m_AssetProvider;
        private ICommandProvider   m_CommandProvider;
        private IUserActorProvider m_UserActorProvider;

        private GameObject m_ViewInstance;

        private IReadOnlyList<IResolvedActorData> m_TargetDeck;
        private IResolvedActorData[]              m_CachedTeamData;

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

            var team = m_UserActorProvider.GetCurrentTeam();
            CardCollectionViewChangeDeckContext context = new CardCollectionViewChangeDeckContext()
            {
                index = idx,
                selected = team[idx],
                team = team,
                data = m_UserActorProvider.PlayerActors
            };

            await EventHandlerProvider.Resolve<CardCollectionViewEvent>()
                .ExecuteAsync(CardCollectionViewEvent.Open, context)
                .AttachExternalCancellation(ReserveToken)
                .SuppressCancellationThrow()
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
                        grade    = actor.Grade,
                        level = actor.Level
                    };
                }

                openContext = new DeckViewOpenContext()
                {
                    actorContexts = actorContexts,
                };
            }

            if (m_ViewInstance is null)
            {
                m_ViewInstance = await ViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, ctx, ReserveToken)
                        .AttachExternalCancellation(ReserveToken)
                    ;
                this.Inject(m_ViewInstance);

                // TODO:
                m_CachedTeamData ??= new IResolvedActorData[UserActorDataSession.TEAM_COUNT];
                m_TargetDeck     =   m_UserActorProvider.GetCurrentTeam();
                m_TargetDeck.CopyTo(m_CachedTeamData);
            }

            foreach (var actorContext in openContext.actorContexts)
            {
                await EventHandler.ExecuteAsync(DeckViewEvent.SetActor, actorContext)
                        .AttachExternalCancellation(ReserveToken)
                        .SuppressCancellationThrow()
                    ;
            }
        }
        private async UniTask OnClose(DeckViewEvent e, object ctx)
        {
            if (m_ViewInstance is null)
                return;

            // TODO: if there is any changes, ask for permanent changes. if not, throw all changes
            await ResetDeckConfirmationAsync();

            this.Detach(m_ViewInstance);
            await ViewProvider.CloseAsync(ctx, ReserveToken)
                .AttachExternalCancellation(ReserveToken)
                .SuppressCancellationThrow()
                ;

            m_ViewInstance = null;
            m_TargetDeck   = null;
            Array.Clear(m_CachedTeamData, 0, m_CachedTeamData.Length);
        }

        private async UniTask ResetDeckConfirmationAsync()
        {
            if (!IsDeckChanged()) return;

            var evHandler = EventHandlerProvider.Resolve<ModalViewEvent>();
            ContentViewEventDelegate<ModalViewEvent>
                confirmAction = async (e, ctx) =>
                {
                    m_UserActorProvider.Enqueue(new FlushUserActorChangeCommand());
                    await m_CommandProvider.EnqueueAsync<RestartStageCommand>(this);

                    await evHandler.ExecuteAsync(ModalViewEvent.Close, new ModalViewCloseContext(0, false))
                        .AttachExternalCancellation(ReserveToken)
                        .SuppressCancellationThrow();

                    "confirm".ToLog();
                },
                cancelAction = async (e, ctx) =>
                {
                    m_UserActorProvider.Enqueue(new ResetUserActorDeckChangeCommand());

                    await evHandler.ExecuteAsync(ModalViewEvent.Close, new ModalViewCloseContext(0, false))
                        .AttachExternalCancellation(ReserveToken)
                        .SuppressCancellationThrow();

                    "cancel".ToLog();
                };
            using var t0 = evHandler.Temp(ModalViewEvent.ConfirmButton, confirmAction);
            using var t1 = evHandler.Temp(ModalViewEvent.CancelButton, cancelAction);

            await evHandler.ExecuteAsync(ModalViewEvent.Open, new ModalView00OpenContext(
                "Confirmation",
                "Deck has been changed. Do you want to change it? This will reset current stage.",
                true, true, true))
                    .AttachExternalCancellation(ReserveToken)
                    .SuppressCancellationThrow()
                ;

            "done".ToLog();
        }

        private bool IsDeckChanged()
        {
            for (int i = 0; i < m_TargetDeck.Count; i++)
            {
                if (m_CachedTeamData[i] != m_TargetDeck[i])
                    return true;
            }

            return false;
        }

        void IConnector<IUserActorProvider>.Connect(IUserActorProvider    t) => m_UserActorProvider = t;
        void IConnector<IUserActorProvider>.Disconnect(IUserActorProvider t) => m_UserActorProvider = null;
        void IConnector<ICommandProvider>.  Connect(ICommandProvider      t) => m_CommandProvider = t;
        void IConnector<ICommandProvider>.  Disconnect(ICommandProvider   t) => m_CommandProvider = null;
    }

    struct ResetUserActorDeckChangeCommand : IQueryCommand<UserActorDataQuery>
    {
        public void Execute(ref UserActorDataQuery query)
        {
            query.ResetData();
        }
    }
}