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
using System.Threading;
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
    public interface IContentViewProvider<TEvent> : IContentViewProvider, IConnector<IContentViewEventHandler<TEvent>>
        where TEvent : struct, IConvertible
    {
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
        /// Opens the ContentViewProvider asynchronously.
        /// </summary>
        /// <param name="canvasProvider">The <see cref="ICanvasViewProvider"/> instance used for opening the ContentViewProvider.</param>
        /// <param name="assetProvider">The <see cref="IAssetProvider"/> instance used for opening the ContentViewProvider.</param>
        /// <param name="ctx">The context object passed to the OpenAsync method.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="UniTask{TResult}"/> representing the asynchronous operation. It completes when the ContentViewProvider is opened.</returns>
        UniTask<GameObject> OpenAsync(
            [NotNull]   ICanvasViewProvider canvasProvider,
            [NotNull]   IAssetProvider      assetProvider,
            [CanBeNull] object              ctx,
            CancellationToken               cancellationToken);

        /// <summary>
        /// Closes the ContentViewProvider asynchronously with the given context.
        /// </summary>
        /// <param name="ctx">The context to close with.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask CloseAsync([CanBeNull] object ctx, CancellationToken cancellationToken);
    }
}