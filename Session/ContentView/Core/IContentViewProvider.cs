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
// File created : 2024, 05, 23 21:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents a content view provider interface.
    /// </summary>
    [AbstractProvider]
    public interface IContentViewProvider<TEvent> : IContentViewProvider
        where TEvent : struct, IConvertible
    {
        /// <summary>
        /// Initializes the ContentViewProvider with the given event handler.
        /// </summary>
        /// <typeparam name="TEvent">The type of event.</typeparam>
        /// <param name="eventHandler">The event handler to initialize with.</param>
        /// <remarks>
        /// This method is used to initialize the ContentViewProvider by providing an event handler.
        /// The event handler is used to handle events specific to the ContentViewProvider.
        /// </remarks>
        void Initialize(IContentViewEventHandler<TEvent> eventHandler);
    }
    /// <summary>
    /// Represents a content view provider interface.
    /// </summary>
    [PublicAPI, AbstractProvider]
    public interface IContentViewProvider : IProvider
    {
        /// <summary>
        /// Represents the type of event associated with a content view provider.
        /// </summary>
        Type EventType { get; }

        /// <summary>
        /// Gets the type of the content view provider.
        /// </summary>
        /// <value>The type of the content view provider.</value>
        Type ProviderType { get; }

        /// <summary>
        /// Reserves the content view provider.
        /// </summary>
        /// <remarks>
        /// This method is used to reserve the content view provider.
        /// </remarks>
        void Reserve();

        /// <summary>
        /// Opens the ContentViewProvider asynchronously.
        /// </summary>
        /// <param name="canvasProvider">The ICanvasViewProvider instance used for opening the ContentViewProvider.</param>
        /// <param name="assetProvider">The IAssetProvider instance used for opening the ContentViewProvider.</param>
        /// <param name="ctx">The context object passed to OpenAsync method.</param>
        /// <returns>A UniTask representing the asynchronous operation. It completes when the ContentViewProvider is opened.</returns>
        UniTask<GameObject> OpenAsync(
            [NotNull]
            ICanvasViewProvider canvasProvider,
            [NotNull]
            IAssetProvider assetProvider,
            [CanBeNull]
            object ctx);

        /// <summary>
        /// Closes the ContentViewProvider asynchronously with the given context.
        /// </summary>
        /// <param name="ctx">The context to close with.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask CloseAsync([CanBeNull] object ctx);
    }
}