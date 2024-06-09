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
        IConnector<ICanvasViewProvider>,
        IConnector<IContentViewEventHandlerProvider>
        where TEvent : struct, IConvertible
    {
        private IContentViewEventHandler m_EventHandler;

        Type IContentViewChildSession.   EventType    => typeof(TEvent);
        IContentViewEventHandler IContentViewChildSession.EventHandler => m_EventHandler;

        /// <summary>
        /// Represents a provider for accessing a canvas view.
        /// </summary>
        protected ICanvasViewProvider CanvasViewProvider { get; private set; }

        /// <summary>
        /// Represents a generic event handler that can be associated with multiple events and executed asynchronously.
        /// </summary>
        protected IContentViewEventHandler<TEvent> EventHandler => (IContentViewEventHandler<TEvent>)m_EventHandler;

        /// <summary>
        /// Represents a provider for accessing event handlers in a content view.
        /// </summary>
        protected IContentViewEventHandlerProvider EventHandlerProvider { get; private set; }

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

        /// <summary>
        /// Creates and returns an instance of an event handler that is used to handle ContentView events.
        /// </summary>
        /// <typeparam name="TEvent">The type of event that the event handler handles.</typeparam>
        /// <returns>An instance of the event handler.</returns>
        protected virtual IContentViewEventHandler<TEvent> CreateEventHandler()
        {
            return new ContentViewEventHandler<TEvent>();
        }

        void IConnector<ICanvasViewProvider>.Connect(ICanvasViewProvider    t) => CanvasViewProvider = t;
        void IConnector<ICanvasViewProvider>.Disconnect(ICanvasViewProvider t) => CanvasViewProvider = null;

        void IConnector<IContentViewEventHandlerProvider>.Connect(IContentViewEventHandlerProvider    t)
        {
            EventHandlerProvider = t;
            EventHandlerProvider.Register(this);
        }
        void IConnector<IContentViewEventHandlerProvider>.Disconnect(IContentViewEventHandlerProvider t)
        {
            EventHandlerProvider.Unregister(this);
            EventHandlerProvider = null;
        }
    }

    /// <summary>
    /// Represents an abstract child session that is used in a content view and can be managed by a parent session.
    /// </summary>
    /// <typeparam name="TEvent">The event type used by the session.</typeparam>
    /// <typeparam name="TProvider">The provider type used by the session.</typeparam>
    [PublicAPI]
    public abstract class ContentViewChildSession<TEvent, TProvider>
        : ContentViewChildSession<TEvent>, IConnector<TProvider>
        where TEvent : struct, IConvertible
        where TProvider : IContentViewProvider<TEvent>
    {
        /// <summary>
        /// Represents a provider for accessing a view.
        /// </summary>
        protected TProvider ViewProvider { get; private set; }

        void IConnector<TProvider>.Connect(TProvider t)
        {
            ViewProvider = t;
            // TODO: inject all dependencies for the view provider
            if (ViewProvider is IConnector<IContentViewEventHandler<TEvent>> ev)
            {
                Connect(ev);
            }
        }
        void IConnector<TProvider>.Disconnect(TProvider t)
        {
            if (ViewProvider is IConnector<IContentViewEventHandler<TEvent>> ev)
            {
                Disconnect(ev);
            }
            ViewProvider = default;
        }
    }
}