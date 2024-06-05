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
// File created : 2024, 05, 29 11:05

#endregion

using System;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents an abstract child session that is used in a content view and can be managed by a parent session.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    [PublicAPI]
    public abstract class ContentViewChildSession<TEvent> :
        ParentSession<ContentViewSessionData>, IContentViewChildSession,
        IConnector<ICanvasViewProvider>

        where TEvent : struct, IConvertible
    {
        private IContentViewEventHandler m_EventHandler;

        Type IContentViewChildSession.   EventType    => typeof(TEvent);
        IContentViewEventHandler IContentViewChildSession.EventHandler => m_EventHandler;

        /// <summary>
        /// Represents a provider for accessing a canvas view.
        /// </summary>
        protected ICanvasViewProvider CanvasViewProvider { get; private set; }
        protected IContentViewEventHandler<TEvent> EventHandler => (IContentViewEventHandler<TEvent>)m_EventHandler;
        protected IContentViewEventHandlerProvider EventHandlerProvider { get; private set; }

        void IConnector<ICanvasViewProvider>.Connect(ICanvasViewProvider t) => CanvasViewProvider = t;
        void IConnector<ICanvasViewProvider>.Disconnect(ICanvasViewProvider t) => CanvasViewProvider = null;

        IContentViewEventHandler IContentViewChildSession.CreateEventHandler()
        {
            if (m_EventHandler is not null)
                throw new InvalidOperationException();

            m_EventHandler = CreateEventHandler();
            Register(typeof(IContentViewEventHandler<TEvent>), m_EventHandler);
            return m_EventHandler;
        }
        void IContentViewChildSession.ReserveEventHandler()
        {
            Unregister(typeof(IContentViewEventHandler<TEvent>));
            m_EventHandler.Dispose();
            m_EventHandler       = null;
            EventHandlerProvider = null;
        }
        void IContentViewChildSession.Setup(IContentViewEventHandlerProvider eventHandlerProvider)
        {
            EventHandlerProvider = eventHandlerProvider;
        }

        protected virtual IContentViewEventHandler<TEvent> CreateEventHandler()
        {
            return new ContentViewEventHandler<TEvent>();
        }
    }

    [PublicAPI]
    public abstract class ContentViewChildSession<TEvent, TProvider>
        : ContentViewChildSession<TEvent>, IConnector<TProvider>

        where TEvent : struct, IConvertible
        where TProvider : IContentViewProvider<TEvent>
    {
        protected TProvider ViewProvider { get; private set; }

        void IConnector<TProvider>.Connect(TProvider t)
        {
            ViewProvider = t;
            ViewProvider.Initialize(EventHandler);
        }
        void IConnector<TProvider>.Disconnect(TProvider t)
        {
            ViewProvider.Reserve();
            ViewProvider = default;
        }
    }
}