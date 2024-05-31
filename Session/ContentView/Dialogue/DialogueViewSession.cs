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
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.Dialogue
{
    [UsedImplicitly]
    public sealed class DialogueViewSession : ContentViewChildSession<DialogueViewSession.SessionData>,
        IDialoguePlayProvider,
        IConnector<IDialogueViewProvider>
    {
        public struct SessionData : ISessionData
        {
            public IContentViewEventHandler<DialogueViewEvent> eventHandler;
            public IContentViewEventHandlerProvider                   viewEventHandlerProvider;
        }

        class DialogueWrapper : IDialogue
        {
            public IDialogueData Data  { get; set; }
            public List<UniTask> Tasks { get; } = new List<UniTask>(32);

            public string        Id => Data.Id;
            public int                               Index => Data.Index;
            public IReadOnlyList<IDialogueAttribute> Attributes => Data.Attributes;
            public IDialogueData                     NextDialogue => Data.NextDialogue;
            public bool IsEnabled(int index)
            {
                return Data.IsEnabled(index);
            }

            public void RegisterTask(UniTask task)
            {
                Tasks.Add(task);
            }
        }

        private IAssetProvider        m_AssetProvider;
        private IDialogueViewProvider m_DialogueViewProvider;

        private CancellationTokenSource m_AttributeSkipToken;

        public override string DisplayName => nameof(DialogueViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            data.eventHandler.Register(DialogueViewEvent.Open, OnOpen);
            data.eventHandler.Register(DialogueViewEvent.Close, OnClose);
            data.eventHandler.Register(DialogueViewEvent.Skip, OnSkip);
        }

        private async UniTask OnClose(DialogueViewEvent e, object ctx)
        {
            if (ctx is not IDialogueData data)
                throw new NotImplementedException();

            await m_DialogueViewProvider.CloseAsync(data);
        }

        private  async UniTask OnSkip(DialogueViewEvent e, object ctx)
        {
            if (m_AttributeSkipToken is null) return;

            m_AttributeSkipToken.Cancel();
        }

        private async UniTask OnOpen(DialogueViewEvent e, object ctx)
        {
            if (ctx is IImmutableObject<DialogueData> da)
            {
                PlayInternal(da.Object).Forget();
                return;
            }
            if (ctx is IDialogueData d)
            {
                PlayInternal(d).Forget();
                return;
            }

            if (ctx is string str)
            {
                if (str.EndsWith(".asset"))
                {
                    PlayInternal(str).Forget();
                }
            }

            throw new NotImplementedException();
        }

        async UniTask IDialoguePlayProvider.Play(IDialogueData dialogue)
        {
            await Data.eventHandler.ExecuteAsync(DialogueViewEvent.Open, dialogue);
        }
        async UniTask IDialoguePlayProvider.Play(string dialogueAssetPath)
        {
            await Data.eventHandler.ExecuteAsync(DialogueViewEvent.Open, dialogueAssetPath);
        }

        private async UniTask PlayInternal(string dialogueAssetPath)
        {
            if (!dialogueAssetPath.EndsWith(".asset"))
                throw new InvalidOperationException($"{dialogueAssetPath}");

            var asset = await m_AssetProvider.LoadAsync<DialogueData>(dialogueAssetPath);
            // await Play(asset.Object);

            await Data.eventHandler.ExecuteAsync(DialogueViewEvent.Open, asset.Object);
        }
        private async UniTask PlayInternal(IDialogueData dialogue)
        {
            UniTask lastCloseTask = UniTask.CompletedTask;

            DialogueProviderResolveDelegate
                resolveProvider = Parent.GetProviderRecursive;

            DialogueWrapper wrapper         = new DialogueWrapper();
            wrapper.Data = dialogue;
            // IDialogueData   currentDialogue = dialogue;

            m_AttributeSkipToken = new CancellationTokenSource();

            while (wrapper.Data != null)
            {
                m_DialogueViewProvider
                    .OpenAsync(CanvasViewProvider, m_AssetProvider, wrapper.Data)
                    .Forget();

                int count = 0;
                foreach (var attribute in wrapper.Attributes)
                {
                    if (!wrapper.IsEnabled(count++)) continue;

                    var task = attribute.ExecuteAsync(
                        new DialogueAttributeContext(
                            wrapper, m_AssetProvider, m_DialogueViewProvider, resolveProvider,
                            Data.viewEventHandlerProvider));

                    // Attributes can be skipped if attribute has SkipAttribute
                    if (attribute is IDialogueSkipAttribute skipAttribute &&
                        skipAttribute.CanSkip)
                    {
                        bool canceled = await task.AttachExternalCancellation(m_AttributeSkipToken.Token)
                            .SuppressCancellationThrow();

                        if (canceled)
                        {
                            await skipAttribute.OnSkip(new DialogueAttributeContext(
                                wrapper, m_AssetProvider, m_DialogueViewProvider, resolveProvider,
                                Data.viewEventHandlerProvider));

                            m_AttributeSkipToken = new CancellationTokenSource();
                            if (skipAttribute.ShouldWaitForInput)
                            {
                                "Wait for another skip event for proceed".ToLog();
                                while (!m_AttributeSkipToken.IsCancellationRequested)
                                {
                                    await UniTask.Yield();
                                }
                            }
                        }

                        m_AttributeSkipToken = new();
                    }
                    else
                        await task;
                }

                while (!m_DialogueViewProvider.IsFullyOpened)
                {
                    await UniTask.Yield();
                }

                IDialogueData prevData = wrapper.Data;
                var newTask = UniTask.WhenAll(wrapper.Tasks.ToArray())
                    // .ContinueWith(async () => await m_DialogueViewProvider.CloseAsync(prevData));
                    .ContinueWith(async () => await Data.eventHandler.ExecuteAsync(DialogueViewEvent.Close, prevData));
                lastCloseTask = UniTask.WhenAll(lastCloseTask, newTask);

                wrapper.Tasks.Clear();
                // lastCloseTask = m_DialogueViewProvider.CloseAsync(wrapper.Data);

                wrapper.Data = wrapper.Data.NextDialogue;
            }
            await lastCloseTask;
        }

        void IConnector<IDialogueViewProvider>.Connect(IDialogueViewProvider    t)
        {
            m_DialogueViewProvider = t;

            m_DialogueViewProvider.Initialize(Data.eventHandler);
        }

        void IConnector<IDialogueViewProvider>.Disconnect(IDialogueViewProvider t)
        {
            m_DialogueViewProvider.Reserve();
            m_DialogueViewProvider = null;
        }
    }
}