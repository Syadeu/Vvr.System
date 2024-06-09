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
// File created : 2024, 05, 29 17:05

#endregion

using System;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents a provider of view event handlers.
    /// </summary>
    [PublicAPI, LocalProvider]
    public interface IContentViewEventHandlerProvider : IProvider
    {
        /// <summary>
        /// Gets the content view event handler for the specified type.
        /// </summary>
        /// <param name="t">The type for which the content view event handler is retrieved.</param>
        /// <returns>The content view event handler for the specified type.</returns>
        IContentViewEventHandler this[Type t] { get; }

        /// <summary>
        /// Registers a child session for the content view.
        /// </summary>
        /// <param name="session">The child session to register.</param>
        void Register(IContentViewChildSession   session);

        /// <summary>
        /// Unregisters a child session from the content view.
        /// </summary>
        /// <param name="session">The child session to unregister.</param>
        void Unregister(IContentViewChildSession session);

        /// <summary>
        /// Resolves the event handler for the specified event type.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <returns>The event handler for the specified event type.</returns>
        IContentViewEventHandler Resolve(Type eventType);
        /// <summary>
        /// Resolves the event handler for the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns>The event handler for the specified event type.</returns>
        IContentViewEventHandler<TEvent> Resolve<TEvent>() where TEvent : struct, IConvertible;
    }
}