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
// File created : 2024, 06, 02 22:06

#endregion

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Provides the base functionality for content view providers.
    /// </summary>
    [DisallowMultipleComponent]
    [PublicAPI]
    public abstract class ContentViewProviderComponent : MonoBehaviour, IContentViewProvider
    {
        public abstract Type EventType    { get; }
        public virtual  Type ProviderType => Vvr.Provider.Provider.ExtractType(GetType());

        public abstract UniTask<GameObject> OpenAsync(
            ICanvasViewProvider canvasProvider,
            IAssetProvider      assetProvider,
            object              ctx,
            CancellationToken cancellationToken);

        public abstract UniTask CloseAsync(object ctx, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Provides the base functionality for content view providers.
    /// </summary>
    [PublicAPI]
    public abstract class ContentViewProviderComponent<TEvent>
        : ContentViewProviderComponent, IConnector<IContentViewEventHandler<TEvent>>
        where TEvent : struct, IConvertible
    {
        public sealed override Type EventType => typeof(TEvent);

        /// <summary>
        /// Represents an event handler interface for ContentView events.
        /// </summary>
        /// <typeparam name="TEvent">The type of event handled by the event handler.</typeparam>
        /// <remarks>
        /// This interface is used to handle ContentView events. It provides methods to register and unregister event delegates, as well as execute events asynchronously.
        /// </remarks>
        public IContentViewEventHandler<TEvent> EventHandler { get; private set; }

        /// <summary>
        /// Called when an event handler connecting to the ContentViewProviderComponent is successfully connected.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="eventHandler">The connected event handler.</param>
        protected virtual void OnEventHandlerConnected(IContentViewEventHandler<TEvent>  eventHandler){}

        /// <summary>
        /// Called when an event handler disconnects from the ContentViewProviderComponent.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="eventHandler">The disconnected event handler.</param>
        protected virtual void OnEventHandlerDisconnect(IContentViewEventHandler<TEvent> eventHandler){}

        void IConnector<IContentViewEventHandler<TEvent>>.Connect(IContentViewEventHandler<TEvent> t)
        {
            EventHandler = t;
            OnEventHandlerConnected(EventHandler);
        }
        void IConnector<IContentViewEventHandler<TEvent>>.Disconnect(IContentViewEventHandler<TEvent> t)
        {
            OnEventHandlerDisconnect(EventHandler);
            EventHandler = null;
        }
    }
}