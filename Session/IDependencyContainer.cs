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
// File created : 2024, 05, 14 20:05

#endregion

using System;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a container for registering and resolving dependencies.
    /// </summary>
    [PublicAPI]
    public interface IDependencyContainer
    {
        /// <summary>
        /// Connects the provider interface to the session.
        /// </summary>
        /// <param name="pType">The type of the provider.</param>
        /// <param name="provider">The instance of the provider.</param>
        /// <remarks>
        /// This method connects the specified provider interface to the session for internal use within the ChildSession class.
        /// It is used to establish communication between the session and the provider.
        /// </remarks>
        IDependencyContainer Register([NotNull] Type pType, [NotNull] IProvider provider);

        /// <summary>
        /// Connects the specified provider interface to the session for internal use within the ChildSession class.
        /// It is used to establish communication between the session and the provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <param name="provider">The instance of the provider.</param>
        /// <returns>The updated instance of the IDependencyContainer.</returns>
        /// <remarks>
        /// This method connects the provider interface to the session.
        /// It is used to establish communication between the session and the provider.
        /// </remarks>
        IDependencyContainer Register<TProvider>([NotNull] TProvider provider) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the provider interface from the session.
        /// </summary>
        /// <param name="pType">The type of the provider interface.</param>
        /// <remarks>
        /// This method disconnects the specified provider interface from the session. It is used to terminate communication between the session and the provider.
        /// </remarks>
        IDependencyContainer Unregister([NotNull] Type pType);

        /// <summary>
        /// Unregisters the specified provider from the dependency container.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to unregister.</typeparam>
        /// <returns>The dependency container instance after unregistering the provider.</returns>
        IDependencyContainer Unregister<TProvider>() where TProvider : IProvider;

        /// <summary>
        /// Connects a connector to the session for the specified provider.
        /// </summary>
        /// <remarks>
        /// If target provider already registerd, connect immediately.
        /// </remarks>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <param name="c">The connector to connect.</param>
        /// <returns>void</returns>
        [PublicAPI]
        [ContractAnnotation("c:null => halt")]
        IDependencyContainer Connect<TProvider>([NotNull] IConnector<TProvider> c) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the specified provider from the game session.
        /// </summary>
        /// <typeparam name="TProvider">The type of provider to disconnect.</typeparam>
        /// <param name="c">The connector of the provider.</param>
        [PublicAPI]
        [ContractAnnotation("c:null => halt")]
        IDependencyContainer Disconnect<TProvider>([NotNull] IConnector<TProvider> c) where TProvider : IProvider;

        /// <summary>
        /// Recursively gets the provider of the specified type.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to retrieve.</typeparam>
        /// <returns>The provider of the specified type if it exists, otherwise null.</returns>
        [PublicAPI, CanBeNull]
        TProvider GetProviderRecursive<TProvider>() where TProvider : class, IProvider;

        /// <summary>
        /// Retrieves a provider recursively from the game session hierarchy based on the specified provider type.
        /// </summary>
        /// <remarks>
        /// This method searches for a provider of the specified type in the current session and its parent sessions recursively.
        /// If a match is found, the provider is returned. Otherwise, null is returned.
        /// </remarks>
        /// <param name="providerType">The type of the provider to retrieve.</param>
        /// <returns>The provider of the specified type, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="providerType"/> is null.</exception>
        [PublicAPI, CanBeNull]
        [ContractAnnotation("providerType:null => halt")]
        IProvider GetProviderRecursive([NotNull] Type providerType);
    }
}