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
// File created : 2024, 05, 23 22:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Delegate for ContentView events.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <param name="e">The event.</param>
    /// <param name="ctx">The context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate UniTask ContentViewEventDelegate<in TEvent>(TEvent e, [CanBeNull] object ctx) where TEvent : struct, IConvertible;

    /// <summary>
    /// Event handler interface for ContentView events.
    /// </summary>
    [PublicAPI, LocalProvider]
    public interface IContentViewEventHandler<TEvent> : IContentViewEventHandler
        where TEvent : struct, IConvertible
    {
        /// <summary>
        /// Registers an event delegate for the specified event.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to register.</typeparam>
        /// <param name="e">The event to register.</param>
        /// <param name="x">The event delegate to be invoked when the event is triggered.</param>
        /// <returns>The updated instance of the <see cref="IContentViewEventHandler{TEvent}"/> interface.</returns>
        [NotNull]
        IContentViewEventHandler<TEvent> Register(TEvent   e, [NotNull] ContentViewEventDelegate<TEvent> x);

        /// <summary>
        /// Unregisters an event delegate for the specified event.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to unregister.</typeparam>
        /// <param name="e">The event to unregister.</param>
        /// <param name="x">The event delegate to be unregistered.</param>
        /// <returns>The updated instance of the <see cref="IContentViewEventHandler{TEvent}"/> interface.</returns>
        [NotNull]
        IContentViewEventHandler<TEvent> Unregister(TEvent e, [NotNull] ContentViewEventDelegate<TEvent> x);

        /// <summary>
        /// Executes the specified event asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to execute.</typeparam>
        /// <param name="e">The event to execute.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        UniTask ExecuteAsync(TEvent e);

        /// <summary>
        /// Executes the specified event asynchronously.
        /// </summary>
        /// <param name="e">The event to execute.</param>
        /// <param name="ctx">The context object to pass to the event handler. Can be null.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous execution of the event.</returns>
        UniTask ExecuteAsync(TEvent e, [CanBeNull] object ctx);
    }

    /// <summary>
    /// Event handler interface for ContentView events.
    /// </summary>
    /// <remarks>
    /// This interface extends the <see cref="IContentViewEventHandler"/> interface and is used for handling specific ContentView events.
    /// It provides methods for registering and unregistering event delegates, as well as executing the events asynchronously.
    /// </remarks>
    [AbstractProvider]
    public interface IContentViewEventHandler : IProvider, IDisposable
    {
        bool Disposed { get; }
    }
}