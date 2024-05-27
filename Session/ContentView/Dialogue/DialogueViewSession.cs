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
// File created : 2024, 05, 26 10:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue
{
    [UsedImplicitly]
    public sealed class DialogueViewSession : ParentSession<DialogueViewSession.SessionData>,
        IDialoguePlayProvider,
        IConnector<IDialogueViewProvider>
    {
        public struct SessionData : ISessionData
        {
            public IContentViewEventHandler<DialogueViewEvent> eventHandler;
        }

        private IAssetProvider        m_AssetProvider;
        private IDialogueViewProvider m_DialogueViewProvider;

        public override string DisplayName => nameof(DialogueViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            data.eventHandler.Register(DialogueViewEvent.Open, OnOpen);
        }

        private async UniTask OnOpen(DialogueViewEvent e, object ctx)
        {
            if (ctx is IDialogueData d)
            {
                Play(d).Forget();
                return;
            }

            if (ctx is string str)
            {
                if (str.EndsWith(".asset"))
                {
                    Play(str).Forget();
                }
            }

            throw new NotImplementedException();
        }

        public async UniTask Play(string dialogueAssetPath)
        {
            if (!dialogueAssetPath.EndsWith(".asset"))
                throw new InvalidOperationException($"{dialogueAssetPath}");

            var asset = await m_AssetProvider.LoadAsync<DialogueData>(dialogueAssetPath);
            await Play(asset.Object);
        }
        public async UniTask Play(IDialogueData dialogue)
        {
            UniTask lastCloseTask = UniTask.CompletedTask;

            IDialogueData currentDialogue = dialogue;
            while (currentDialogue != null)
            {
                m_DialogueViewProvider.Open(m_AssetProvider, currentDialogue);

                foreach (var attribute in currentDialogue.Attributes)
                {
                    await attribute.ExecuteAsync(currentDialogue, m_AssetProvider, m_DialogueViewProvider);
                }

                while (!m_DialogueViewProvider.IsFullyOpened)
                {
                    await UniTask.Yield();
                }
                lastCloseTask = m_DialogueViewProvider.Close(currentDialogue);
                currentDialogue = currentDialogue.NextDialogue;
            }

            await lastCloseTask;
        }

        void IConnector<IDialogueViewProvider>.Connect(IDialogueViewProvider    t)
        {
            m_DialogueViewProvider = t;

            m_DialogueViewProvider.Initialize(Data.eventHandler);
        }

        void IConnector<IDialogueViewProvider>.Disconnect(IDialogueViewProvider t) => m_DialogueViewProvider = null;
    }
}