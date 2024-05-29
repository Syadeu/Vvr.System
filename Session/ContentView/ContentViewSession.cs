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
            private readonly Dictionary<TEvent, List<uint>>                     m_Actions   = new();
            private readonly Dictionary<uint, ContentViewEventDelegate<TEvent>> m_ActionMap = new();

            private readonly CancellationTokenSource m_CancellationTokenSource = new();

            private uint CalculateHash(ContentViewEventDelegate<TEvent> x)
            {
                uint hash =
                    FNV1a32.Calculate(x.Method.Name)
                    ^ 267;
                if (x.Method.DeclaringType is not null)
                    hash ^= FNV1a32.Calculate(x.Method.DeclaringType.FullName);
                if (x.Target is not null)
                    hash ^= unchecked((uint)x.Target.GetHashCode());

                return hash;
            }
            public IContentViewEventHandler<TEvent> Register(TEvent e, ContentViewEventDelegate<TEvent> x)
            {
                if (!m_Actions.TryGetValue(e, out var list))
                {
                    list         = new(8);
                    m_Actions[e] = list;
                }

                uint hash = CalculateHash(x);
                if (!m_ActionMap.TryAdd(hash, x))
                {
                    throw new InvalidOperationException("hash collusion");
                }
                m_ActionMap[hash] = x;

                list.Add(hash);
                return this;
            }

            public IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return this;

                uint hash = CalculateHash(x);
                if (!m_ActionMap.Remove(hash)) return this;

                list.Remove(hash);
                return this;
            }

            public async UniTask ExecuteAsync(TEvent e)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return;

                UniTask sum = UniTask.CompletedTask;
                for (int i = 0; i < list.Count; i++)
                {
                    sum = UniTask.WhenAll(sum, m_ActionMap[list[i]](e, null));
                }

                await sum;
            }

            public async UniTask ExecuteAsync(TEvent e, object ctx)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return;

                UniTask sum = UniTask.CompletedTask;
                for (int i = 0; i < list.Count; i++)
                {
                    sum = UniTask.WhenAll(sum, m_ActionMap[list[i]](e, ctx));
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
                m_ActionMap.Clear();
            }
        }

        class ViewEventHandlerProvider : IContentViewEventHandlerProvider, IDisposable
        {
            public IContentViewEventHandler<ResearchViewEvent>        Research        { get; set; }
            public IContentViewEventHandler<DialogueViewEvent>        Dialogue        { get; set; }
            public IContentViewEventHandler<MainmenuViewEvent>        Mainmenu        { get; set; }
            public IContentViewEventHandler<WorldBackgroundViewEvent> WorldBackground { get; set; }

            public void Dispose()
            {
                Research?.Dispose();
                Dialogue?.Dispose();
                Mainmenu?.Dispose();
                WorldBackground?.Dispose();

                Research        = null;
                Dialogue        = null;
                Mainmenu        = null;
                WorldBackground = null;
            }
        }

        private          IContentViewRegistryProvider m_ContentViewRegistryProvider;
        private readonly ViewEventHandlerProvider     m_ViewEventHandlerProvider = new();

        public override string DisplayName => nameof(ContentViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_ViewEventHandlerProvider.Research        = new ContentViewEventHandler<ResearchViewEvent>();
            m_ViewEventHandlerProvider.Dialogue        = new ContentViewEventHandler<DialogueViewEvent>();
            m_ViewEventHandlerProvider.Mainmenu        = new ContentViewEventHandler<MainmenuViewEvent>();
            m_ViewEventHandlerProvider.WorldBackground = new ContentViewEventHandler<WorldBackgroundViewEvent>();

            var canvasSession = await CreateSession<CanvasViewSession>(default);
            Register<ICanvasViewProvider>(canvasSession);

            var researchViewSession = await CreateSession<ResearchViewSession>(
                new ResearchViewSession.SessionData()
                {
                    eventHandler = m_ViewEventHandlerProvider.Research
                });
            var dialogueViewSession = await CreateSession<DialogueViewSession>(
                new DialogueViewSession.SessionData()
                {
                    eventHandler = m_ViewEventHandlerProvider.Dialogue,
                    viewEventHandlerProvider = m_ViewEventHandlerProvider
                });
            var mainmenuViewSession = await CreateSession<MainmenuViewSession>(
                new MainmenuViewSession.SessionData()
                {
                    eventHandler = m_ViewEventHandlerProvider.Mainmenu,
                    researchEventHandler = researchViewSession.Data.eventHandler,
                    dialogueEventHandler = dialogueViewSession.Data.eventHandler
                });
            var worldBackgroundViewSession = await CreateSession<WorldBackgroundViewSession>(
                new WorldBackgroundViewSession.SessionData()
                {
                    eventHandler = m_ViewEventHandlerProvider.WorldBackground
                });

            Parent
                .Register<IDialoguePlayProvider>(dialogueViewSession)
                .Register<IContentViewEventHandlerProvider>(m_ViewEventHandlerProvider)
                ;

            Vvr.Provider.Provider.Static.Connect<IContentViewRegistryProvider>(this);
        }

        protected override async UniTask OnReserve()
        {
            m_ViewEventHandlerProvider.Dispose();

            Parent
                .Unregister<IDialoguePlayProvider>()
                .Unregister<IContentViewEventHandlerProvider>()
                ;

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