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
// File created : 2024, 06, 02 21:06

#endregion

using System;
using JetBrains.Annotations;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents a child session for content view.
    /// </summary>
    public interface IContentViewChildSession : IChildSession
    {
        /// <summary>
        /// Represents the type of event for a child session in a content view.
        /// </summary>
        /// <remarks>
        /// The EventType property is used to define the specific type of event that a child session in a content view can handle. It is implemented by classes derived from the <see cref="IContentViewChildSession"/> interface.
        /// </remarks>
        [NotNull]
        Type EventType { get; }

        /// <summary>
        /// Represents a method that handles an event.
        /// </summary>
        IContentViewEventHandler EventHandler { get; }

        /// <summary>
        /// Creates and returns an instance of an event handler for the content view child session.
        /// </summary>
        /// <returns>An instance of <see cref="IContentViewEventHandler"/>.</returns>
        [NotNull]
        IContentViewEventHandler CreateEventHandler();

        /// <summary>
        /// Reserves the event handler for the content view child session.
        /// </summary>
        void ReserveEventHandler();

        /// <summary>
        /// Sets up the event handler provider for the content view child session.
        /// </summary>
        /// <param name="eventHandlerProvider">The event handler provider to set up.</param>
        void Setup(IContentViewEventHandlerProvider eventHandlerProvider);
    }
}