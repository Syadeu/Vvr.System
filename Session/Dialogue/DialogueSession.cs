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
// File created : 2024, 05, 20 23:05
#endregion

using System.Buffers;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Controller.Asset;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Dialogue
{
    [UsedImplicitly]
    public class DialogueSession : ParentSession<DialogueSession.SessionData>,
        IConnector<IDialogueViewProvider>,
        IConnector<IActorDataProvider>
    {
        public struct SessionData : ISessionData
        {
            public IDialogueData dialogue;
        }

        public override string DisplayName => nameof(DialogueSession);

        private IDialogueViewProvider m_DialogueViewProvider;
        private IActorDataProvider    m_ActorDataProvider;

        private AssetController m_AssetController;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            // Because most likely dialogue uses their own resources
            var assetSession = await CreateSession<AssetSession>(default);
            Register<IAssetProvider>(assetSession);

            m_AssetController = new(data.dialogue.Assets);

            UniTask<IImmutableObject<Sprite>>[] preloadedPortraits
                = new UniTask<IImmutableObject<Sprite>>[data.dialogue.Speakers.Count];
            for (int i = 0; i < data.dialogue.Speakers.Count; i++)
            {
                var speaker  = data.dialogue.Speakers[i];
                var portrait
                    = assetSession.LoadAsync<Sprite>(speaker.Actor.Assets[AssetType.CardPortrait]);

                preloadedPortraits[i] = portrait;
            }

            IImmutableObject<Sprite> backgroundImg = await assetSession.LoadAsync<Sprite>(m_AssetController[AssetType.BackgroundImage]);
            await m_DialogueViewProvider.OpenAsync(backgroundImg.Object);

            for (var i = 0; i < data.dialogue.Speakers.Count; i++)
            {
                var speaker     = data.dialogue.Speakers[i];
                var portraitImg = await preloadedPortraits[i];

                await m_DialogueViewProvider.SpeakAsync(
                    portraitImg.Object,
                    speaker);
            }

            await m_DialogueViewProvider.CloseAsync();
        }

        protected override async UniTask OnReserve()
        {
            // We don't need to manually release registered providers
            // only for following code of conduct which is Dispose pattern
            Unregister<IAssetProvider>();

            await base.OnReserve();
        }

        void IConnector<IDialogueViewProvider>.Connect(IDialogueViewProvider    t) => m_DialogueViewProvider = t;
        void IConnector<IDialogueViewProvider>.Disconnect(IDialogueViewProvider t) => m_DialogueViewProvider = null;
        void IConnector<IActorDataProvider>.   Connect(IActorDataProvider       t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.   Disconnect(IActorDataProvider    t) => m_ActorDataProvider = null;
    }

    public interface IDialogueViewProvider : IProvider
    {
        UniTask OpenAsync(Sprite backgroundImage);

        UniTask SpeakAsync(Sprite portraitImage, IDialogueSpeaker speaker);
        UniTask CloseAsync();
    }
}