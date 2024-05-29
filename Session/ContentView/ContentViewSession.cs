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
// File created : 2024, 05, 23 23:05

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.ContentView.BattleSign;
using Vvr.Session.ContentView.Canvas;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Mainmenu;
using Vvr.Session.ContentView.Provider;
using Vvr.Session.ContentView.Research;
using Vvr.Session.ContentView.WorldBackground;

namespace Vvr.Session.ContentView
{
    /// <summary>
    /// Represents a session for content view functionality.
    /// </summary>
    [UsedImplicitly]
    public sealed class ContentViewSession : ParentSession<ContentViewSession.SessionData>,
        IConnector<IContentViewRegistryProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        class ContentViewEventHandler<TEvent> : IContentViewEventHandler<TEvent>
            where TEvent : struct, IConvertible
        {
            private readonly Dictionary<TEvent, LinkedList<ContentViewEventDelegate<TEvent>>> m_Actions = new();

            private readonly CancellationTokenSource m_CancellationTokenSource = new();

            public IContentViewEventHandler<TEvent> Register(TEvent e, ContentViewEventDelegate<TEvent> x)
            {
                if (!m_Actions.TryGetValue(e, out var list))
                {
                    list         = new();
                    m_Actions[e] = list;
                }

                list.AddLast(x);
                return this;
            }

            public IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return this;

                list.Remove(x);
                return this;
            }

            public async UniTask ExecuteAsync(TEvent e)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return;

                UniTask sum = UniTask.CompletedTask;
                for (LinkedListNode<ContentViewEventDelegate<TEvent>> item = list.First;
                     item != null;
                     item = item.Next)
                {
                    var t0 = item.Value(e, null);
                    sum = UniTask.WhenAll(sum, t0);
                }

                await sum;
            }

            public async UniTask ExecuteAsync(TEvent e, object ctx)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return;

                UniTask sum = UniTask.CompletedTask;
                for (var item = list.First;
                     item != null;
                     item = item.Next)
                {
                    var t0 = item.Value(e, ctx);
                    sum = UniTask.WhenAll(sum, t0);
                }

                await sum;
            }

            public void Dispose()
            {
                m_CancellationTokenSource.Cancel();

                foreach (var list in m_Actions.Values)
                {
                    list.Clear();
                }

                m_Actions.Clear();
            }
        }

        private IContentViewRegistryProvider m_ContentViewRegistryProvider;

        public override string DisplayName => nameof(ContentViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var canvasSession = await CreateSession<CanvasViewSession>(default);
            Register<ICanvasViewProvider>(canvasSession);

            var researchViewSession = await CreateSession<ResearchViewSession>(
                new ResearchViewSession.SessionData()
                {
                    eventHandler = new ContentViewEventHandler<ResearchViewEvent>()
                });
            var dialogueViewSession = await CreateSession<DialogueViewSession>(
                new DialogueViewSession.SessionData()
                {
                    eventHandler = new ContentViewEventHandler<DialogueViewEvent>()
                });
            var mainmenuViewSession = await CreateSession<MainmenuViewSession>(
                new MainmenuViewSession.SessionData()
                {
                    eventHandler = new ContentViewEventHandler<MainmenuViewEvent>(),
                    researchEventHandler = researchViewSession.Data.eventHandler,
                    dialogueEventHandler = dialogueViewSession.Data.eventHandler
                });

            Parent.Register<IDialoguePlayProvider>(dialogueViewSession);

            Vvr.Provider.Provider.Static.Connect<IContentViewRegistryProvider>(this);
        }

        protected override async UniTask OnReserve()
        {
            Parent.Unregister<IDialoguePlayProvider>();

            Vvr.Provider.Provider.Static.Disconnect<IContentViewRegistryProvider>(this);

            await base.OnReserve();
        }

        public void Connect(IContentViewRegistryProvider t)
        {
            m_ContentViewRegistryProvider = t;

            // ReSharper disable RedundantTypeArgumentsOfMethod

            Register<IResearchViewProvider>(t.ResearchViewProvider)
                .Register<IDialogueViewProvider>(t.DialogueViewProvider)
                .Register<IWorldBackgroundViewProvider>(t.WorldBackgroundViewProvider)
                .Register<IBattleSignViewProvider>(t.BattleSignViewProvider)
                .Register<IMainmenuViewProvider>(t.MainmenuViewProvider)
                ;
            // ReSharper restore RedundantTypeArgumentsOfMethod
        }

        public void Disconnect(IContentViewRegistryProvider t)
        {
            Unregister<IResearchViewProvider>()
                .Unregister<IDialogueViewProvider>()
                .Unregister<IWorldBackgroundViewProvider>()
                .Unregister<IBattleSignViewProvider>()
                .Unregister<IMainmenuViewProvider>()
                ;

            m_ContentViewRegistryProvider = null;
        }
    }
}