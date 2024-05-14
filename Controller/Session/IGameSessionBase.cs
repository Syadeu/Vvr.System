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

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Provider;

namespace Vvr.Controller.Session
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
        /// <returns>A UniTask representing the completion of the initialization.</returns>
        UniTask Initialize(Owner owner);

        /// <summary>
        /// Reserves the game session.
        /// </summary>
        /// <returns>A UniTask representing the completion of reserving the game session.</returns>
        UniTask Reserve();

        /// <summary>
        /// Connects the specified provider to the game session.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <param name="provider">The provider instance to connect.</param>
        [PublicAPI]
        void Register<TProvider>(TProvider provider) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the specified provider from the game session.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <remarks>
        /// This method disconnects the specified provider from the game session. The provider must implement the <see cref="IProvider"/> interface.
        /// </remarks>
        /// <seealso cref="Register{TProvider}"/>
        [PublicAPI]
        void Unregister<TProvider>() where TProvider : IProvider;

        [PublicAPI]
        void Connect<TProvider>(IConnector<TProvider>    c) where TProvider : IProvider;
        [PublicAPI]
        void Disconnect<TProvider>(IConnector<TProvider> c) where TProvider : IProvider;
    }
}