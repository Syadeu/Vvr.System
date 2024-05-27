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
// File created : 2024, 05, 10 20:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a base interface for a game session.
    /// </summary>
    [RequireImplementors]
    public interface IGameSessionBase
    {
        /// <summary>
        /// Initializes the game session with the specified owner.
        /// </summary>
        /// <param name="owner">The owner of the game session.</param>
        /// <param name="parent">The parent session.</param>
        /// <param name="data">The session data.</param>
        /// <returns>A UniTask representing the completion of the initialization.</returns>
        UniTask Initialize(Owner owner, [CanBeNull] IParentSession parent, [CanBeNull] ISessionData data);

        /// <summary>
        /// Reserves the game session.
        /// </summary>
        /// <returns>A UniTask representing the completion of reserving the game session.</returns>
        UniTask Reserve();

        /// <summary>
        /// Connects the specified provider to the game session.
        /// </summary>
        /// <remarks>
        /// If connector found, also connect provider respectfully.
        /// </remarks>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <param name="provider">The provider instance to connect.</param>
        [PublicAPI]
        IGameSessionBase Register<TProvider>([NotNull] TProvider provider) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the specified provider from the game session.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <remarks>
        /// This method disconnects the specified provider from the game session.
        /// The provider must implement the <see cref="IProvider"/> interface.
        ///
        /// If there is relevant connectors also disconnect respectfully.
        /// </remarks>
        /// <seealso cref="Register{TProvider}"/>
        [PublicAPI]
        IGameSessionBase Unregister<TProvider>() where TProvider : IProvider;

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
        IGameSessionBase Connect<TProvider>([NotNull] IConnector<TProvider>    c) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the specified provider from the game session.
        /// </summary>
        /// <typeparam name="TProvider">The type of provider to disconnect.</typeparam>
        /// <param name="c">The connector of the provider.</param>
        [PublicAPI]
        IGameSessionBase Disconnect<TProvider>([NotNull] IConnector<TProvider> c) where TProvider : IProvider;

        /// <summary>
        /// Recursively gets the provider of the specified type.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to retrieve.</typeparam>
        /// <returns>The provider of the specified type if it exists, otherwise null.</returns>
        [PublicAPI, CanBeNull]
        TProvider GetProviderRecursive<TProvider>() where TProvider : class, IProvider;

        [PublicAPI, CanBeNull]
        IProvider GetProviderRecursive([NotNull] Type providerType);
    }
}