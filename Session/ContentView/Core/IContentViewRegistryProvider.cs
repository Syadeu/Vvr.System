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
// File created : 2024, 05, 24 00:05

#endregion

using System;
using System.Collections.Generic;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents a provider interface for managing content view providers.
    /// </summary>
    public interface IContentViewRegistryProvider : IProvider
    {
        /// <summary>
        /// Represents a provider interface for managing content view providers.
        /// </summary>
        IReadOnlyDictionary<Type, IContentViewProvider> Providers { get; }

        /// <summary>
        /// Resolves the content view provider for the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns>The content view provider for the specified event type.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the specified event type is not registered.</exception>
        IContentViewProvider Resolve<TEvent>() where TEvent : struct, IConvertible;
    }
}