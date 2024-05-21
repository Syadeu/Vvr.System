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
using UnityEngine.Assertions;
using Vvr.Controller.Asset;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Dialogue
{
    [UsedImplicitly]
    public class DialogueSession : ParentSession<DialogueSession.SessionData>,
        IConnector<IAssetProvider>,
        IConnector<IDialogueViewProvider>,
        IConnector<IActorDataProvider>
    {
        public struct SessionData : ISessionData
        {
            public IDialogueData dialogue;

            public SessionData(IDialogueData d)
            {
                dialogue = d;
            }
        }

        public override string DisplayName => nameof(DialogueSession);

        private IAssetProvider        m_AssetProvider;
        private IDialogueViewProvider m_DialogueViewProvider;
        private IActorDataProvider    m_ActorDataProvider;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            Assert.IsNotNull(m_AssetProvider);
            Assert.IsNotNull(m_ActorDataProvider);
            Assert.IsNotNull(m_ActorDataProvider.DataSheet);

            data.dialogue.Build(m_ActorDataProvider.DataSheet);

            UniTask<IImmutableObject<Sprite>>[] preloadedPortraits
                = new UniTask<IImmutableObject<Sprite>>[data.dialogue.Speakers.Count];
            for (int i = 0; i < data.dialogue.Speakers.Count; i++)
            {
                var speaker  = data.dialogue.Speakers[i];
                if (speaker.Actor == null) continue;

                UniTask<IImmutableObject<Sprite>> portrait;
                if (speaker.OverridePortrait.RuntimeKeyIsValid())
                {
                    portrait = m_AssetProvider.LoadAsync<Sprite>(speaker.OverridePortrait);
                }
                else
                    portrait = m_AssetProvider.LoadAsync<Sprite>(speaker.Actor.Assets[AssetType.DialoguePortrait]
                        .FullPath);

                preloadedPortraits[i] = portrait;
            }

            IImmutableObject<Sprite> backgroundImg = await m_AssetProvider.LoadAsync<Sprite>(
                data.dialogue.Assets[AssetType.BackgroundImage]);
            await m_DialogueViewProvider.OpenAsync(data.dialogue.Id, backgroundImg?.Object);

            for (var i = 0; i < data.dialogue.Speakers.Count; i++)
            {
                var speaker     = data.dialogue.Speakers[i];
                var portraitImg = await preloadedPortraits[i];

                $"[Dialogue] Speak {i}".ToLog();
                await m_DialogueViewProvider.SpeakAsync(
                    portraitImg?.Object,
                    speaker);

                await UniTask.WaitForSeconds(speaker.Time);
            }

            await m_DialogueViewProvider.CloseAsync(data.dialogue.Id);
        }

        protected override async UniTask OnReserve()
        {
            // We don't need to manually release registered providers
            // only for following code of conduct which is Dispose pattern
            Unregister<IAssetProvider>();

            await base.OnReserve();
        }

        void IConnector<IAssetProvider>.       Connect(IAssetProvider           t) => m_AssetProvider = t;
        void IConnector<IAssetProvider>.       Disconnect(IAssetProvider        t) => m_AssetProvider = null;
        void IConnector<IDialogueViewProvider>.Connect(IDialogueViewProvider    t) => m_DialogueViewProvider = t;
        void IConnector<IDialogueViewProvider>.Disconnect(IDialogueViewProvider t) => m_DialogueViewProvider = null;
        void IConnector<IActorDataProvider>.   Connect(IActorDataProvider       t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.   Disconnect(IActorDataProvider    t) => m_ActorDataProvider = null;
    }
}