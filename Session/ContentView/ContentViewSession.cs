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
using Vvr.Provider;
using Vvr.Session.ContentView.Research;

namespace Vvr.Session.ContentView
{
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

            public async UniTask Execute(TEvent e)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return;

                await UniTask
                        .WhenAll(list.Select(x => x(e, null)))
                        .AttachExternalCancellation(m_CancellationTokenSource.Token)
                        .SuppressCancellationThrow()
                    ;
            }
            public async UniTask Execute(TEvent e, object ctx)
            {
                if (!m_Actions.TryGetValue(e, out var list)) return;

                // foreach (var item in list)
                // {
                //     await item(e, ctx);
                // }
                await UniTask
                        .WhenAll(list.Select(x => x(e, ctx)))
                        .AttachExternalCancellation(m_CancellationTokenSource.Token)
                        .SuppressCancellationThrow()
                    ;
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

        private IChildSession
            m_ResearchViewSession;

        private IContentViewEventHandler<ResearchViewEvent>
            m_ResearchViewEventHandler;

        public override string DisplayName => nameof(ContentViewSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            Vvr.Provider.Provider.Static.Connect<IContentViewRegistryProvider>(this);
        }
        protected override async UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IContentViewRegistryProvider>(this);

            m_ResearchViewEventHandler?.Dispose();

            m_ResearchViewEventHandler = null;

            await base.OnReserve();
        }

        public void Connect(IContentViewRegistryProvider t)
        {
            m_ContentViewRegistryProvider = t;

            Register(t.ResearchViewProvider);
        }
        public void Disconnect(IContentViewRegistryProvider t)
        {
            Unregister(t.ResearchViewProvider);

            m_ContentViewRegistryProvider = null;
        }

        void Register(IResearchViewProvider t)
        {
            if (m_ResearchViewEventHandler != null)
                throw new InvalidOperationException();

            var evHandler = new ContentViewEventHandler<ResearchViewEvent>();
            m_ResearchViewEventHandler = evHandler;

            evHandler.Register(ResearchViewEvent.Open, async (ev, ctx) =>
            {
                m_ResearchViewSession = await CreateSession<ResearchViewSession>(
                    new ResearchViewSession.SessionData()
                    {
                        eventHandler = m_ResearchViewEventHandler
                    });
            });
            evHandler.Register(ResearchViewEvent.Close, async (ev, ctx) =>
            {
                await m_ResearchViewSession.Reserve();
                m_ResearchViewSession = null;
            });

            t.Initialize(evHandler);

            base.Register<IResearchViewProvider>(t);
        }
        void Unregister(IResearchViewProvider t)
        {
            if (m_ResearchViewEventHandler == null)
                throw new InvalidOperationException();

            m_ResearchViewEventHandler.Dispose();
            m_ResearchViewEventHandler = null;

            base.Unregister<IResearchViewProvider>();
        }
    }
}