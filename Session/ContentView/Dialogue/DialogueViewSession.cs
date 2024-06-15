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
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.Dialogue
{
    [UsedImplicitly]
    public sealed class DialogueViewSession
        : ContentViewChildSession<DialogueViewEvent, IDialogueViewProvider>, IDialoguePlayProvider
    {
        class DialogueWrapper : IDialogue
        {
            public IDialogueData Data  { get; set; }
            public List<UniTask> Tasks { get; } = new List<UniTask>(32);

            public string        Id => Data.Id;
            public IReadOnlyList<IDialogueAttribute> Attributes => Data.Attributes;
            public IDialogueData                     NextDialogue => Data.NextDialogue;

            public void RegisterTask(UniTask task)
            {
                Tasks.Add(task);
            }
        }

        private IAssetProvider m_AssetProvider;

        private CancellationTokenSource m_AttributeSkipToken;

        public override string DisplayName => nameof(DialogueViewSession);

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);
            Register(m_AssetProvider);

            EventHandler
                .Register(DialogueViewEvent.Open, OnOpen)
                .Register(DialogueViewEvent.Close, OnClose)
                .Register(DialogueViewEvent.Skip, OnSkip);
        }

        private async UniTask OnClose(DialogueViewEvent e, object ctx)
        {
            if (ctx is not IDialogueData data)
                throw new NotImplementedException();

            await ViewProvider.CloseAsync(data, ReserveToken);
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
                await PlayInternal(da.Object);
                return;
            }
            if (ctx is IDialogueData d)
            {
                await PlayInternal(d);
                return;
            }

            if (ctx is string str)
            {
                if (str.EndsWith(".asset"))
                {
                    await PlayInternal(str);
                    return;
                }
            }

            throw new NotImplementedException($"{ctx?.GetType().FullName}");
        }

        async UniTask IDialoguePlayProvider.Play(IDialogueData dialogue)
        {
            await EventHandler.ExecuteAsync(DialogueViewEvent.Open, dialogue);
        }
        async UniTask IDialoguePlayProvider.Play(string dialogueAssetPath)
        {
            await EventHandler.ExecuteAsync(DialogueViewEvent.Open, dialogueAssetPath);
        }

        private async UniTask PlayInternal(string dialogueAssetPath)
        {
            if (!dialogueAssetPath.EndsWith(".asset"))
                throw new InvalidOperationException($"{dialogueAssetPath}");

            var asset = await m_AssetProvider.LoadAsync<DialogueData>(dialogueAssetPath);

            await EventHandler.ExecuteAsync(DialogueViewEvent.Open, asset.Object);
        }
        private async UniTask PlayInternal(IDialogueData dialogue)
        {
            UniTask lastCloseTask = UniTask.CompletedTask;

            DialogueProviderResolveDelegate
                resolveProvider = Parent.GetProviderRecursive;

            DialogueWrapper wrapper = new DialogueWrapper();
            wrapper.Data = dialogue;

            m_AttributeSkipToken = new CancellationTokenSource();

            while (wrapper.Data != null)
            {
                var obj = await ViewProvider
                    .OpenAsync(CanvasViewProvider, m_AssetProvider, wrapper.Data, ReserveToken);
                this.Inject(obj);

                foreach (var attribute in wrapper.Attributes)
                {
                    if (attribute is null) continue;

                    var task = attribute.ExecuteAsync(
                        new DialogueAttributeContext(
                            wrapper, m_AssetProvider, ViewProvider, resolveProvider,
                            EventHandlerProvider,
                            ReserveToken));

                    // Attributes can be skipped if attribute has SkipAttribute
                    if (attribute is IDialogueSkipAttribute skipAttribute &&
                        skipAttribute.CanSkip)
                    {
                        bool canceled = await task
                            .AttachExternalCancellation(m_AttributeSkipToken.Token)
                            .SuppressCancellationThrow();

                        if (canceled)
                        {
                            await skipAttribute.OnSkip(new DialogueAttributeContext(
                                wrapper, m_AssetProvider, ViewProvider, resolveProvider,
                                EventHandlerProvider, ReserveToken))
                                .AttachExternalCancellation(ReserveToken)
                                .SuppressCancellationThrow();

                            m_AttributeSkipToken = new CancellationTokenSource();
                            if (skipAttribute.ShouldWaitForInput &&
                                !ReserveToken.IsCancellationRequested)
                            {
                                "Wait for another skip event for proceed".ToLog();
                                while (!m_AttributeSkipToken.IsCancellationRequested &&
                                       !ReserveToken.IsCancellationRequested)
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

                while (!ViewProvider.IsFullyOpened && !ReserveToken.IsCancellationRequested)
                {
                    await UniTask.Yield();
                }

                IDialogueData prevData = wrapper.Data;
                var newTask = UniTask.WhenAll(wrapper.Tasks.ToArray())
                    .ContinueWith(async () =>
                    {
                        this.Detach(obj);
                        await EventHandler.ExecuteAsync(DialogueViewEvent.Close, prevData);
                    });
                lastCloseTask = UniTask.WhenAll(lastCloseTask, newTask);

                wrapper.Tasks.Clear();
                wrapper.Data = wrapper.Data.NextDialogue;
            }
            await lastCloseTask
                .AttachExternalCancellation(ReserveToken)
                .SuppressCancellationThrow()
                ;
        }
    }
}