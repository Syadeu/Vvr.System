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
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.ContentView.Canvas;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Deck;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Mainmenu;
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

        class ViewEventHandlerProvider : IContentViewEventHandlerProvider, IDisposable
        {
            public Dictionary<Type, IContentViewEventHandler> ViewEventHandlers { get; } = new();

            public IContentViewEventHandler this[Type t]
            {
                get => Resolve(t);
                set => ViewEventHandlers[t] = value;
            }

            public IContentViewEventHandler Resolve(Type eventType)
            {
                EvaluateEventType(eventType);
                return ViewEventHandlers[eventType];
            }
            public IContentViewEventHandler<TEvent> Resolve<TEvent>() where TEvent : struct, IConvertible
            {
                return (IContentViewEventHandler<TEvent>)Resolve(VvrTypeHelper.TypeOf<TEvent>.Type);
            }

            [Conditional("UNITY_EDITOR")]
            [Conditional("DEVELOPMENT_BUILD")]
            private static void EvaluateEventType(Type t)
            {
                if (!t.IsEnum)
                    throw new InvalidOperationException(t.FullName);
            }

            public void Dispose()
            {
                foreach (var v in ViewEventHandlers.Values)
                {
                    v.Dispose();
                }
                ViewEventHandlers.Clear();
            }
        }

        private          IContentViewRegistryProvider m_ContentViewRegistryProvider;
        private readonly ViewEventHandlerProvider     m_ViewEventHandlerProvider = new();

        public override string DisplayName => nameof(ContentViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var canvasSession = await CreateSession<CanvasViewSession>(default);
            Register<ICanvasViewProvider>(canvasSession);

            await UniTask.WhenAll(
                CreateSession<ResearchViewSession>(null),
                CreateSession<MainmenuViewSession>(null),
                CreateSession<WorldBackgroundViewSession>(null),
                CreateSession<DeckViewSession>(null)
            );
            var dialogueViewSession = await CreateSession<DialogueViewSession>(null);

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

        protected override UniTask OnCreateSession(IChildSession session)
        {
            if (session is IContentViewChildSession childSession)
            {
                m_ViewEventHandlerProvider[childSession.EventType]
                    = childSession.CreateEventHandler();

                childSession.Setup(m_ViewEventHandlerProvider);
            }
            return base.OnCreateSession(session);
        }
        protected override UniTask OnSessionClose(IChildSession session)
        {
            if (session is IContentViewChildSession childSession)
            {
                childSession.ReserveEventHandler();
                m_ViewEventHandlerProvider.ViewEventHandlers.Remove(childSession.EventType);
            }
            return base.OnSessionClose(session);
        }

        void IConnector<IContentViewRegistryProvider>.Connect(IContentViewRegistryProvider t)
        {
            m_ContentViewRegistryProvider = t;

            foreach (var item in t.Providers.Values)
            {
                Register(item.ProviderType, item);
            }
        }
        void IConnector<IContentViewRegistryProvider>.Disconnect(IContentViewRegistryProvider t)
        {
            foreach (var item in t.Providers.Values)
            {
                Unregister(item.ProviderType);
            }

            m_ContentViewRegistryProvider = null;
        }
    }
}