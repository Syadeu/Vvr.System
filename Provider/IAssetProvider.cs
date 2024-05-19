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
// File created : 2024, 05, 17 02:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents an asset provider interface used to load assets asynchronously.
    /// </summary>
    /// <remarks>
    /// This interface is marked with the <see cref="LocalProviderAttribute"/> attribute,
    /// indicating that it is specific to a particular module or component and not intended to be used as a global provider.
    /// </remarks>
    [LocalProvider]
    public interface IAssetProvider : IProvider
    {
        /// <summary>
        /// Loads an asset asynchronously from the asset provider.
        /// </summary>
        /// <typeparam name="TObject">The type of the asset.</typeparam>
        /// <param name="key">The key used to identify the asset.</param>
        /// <returns>A task representing the asynchronous loading of the asset.</returns>
        [PublicAPI]
        UniTask<IImmutableObject<TObject>> LoadAsync<TObject>([CanBeNull] object key) where TObject : UnityEngine.Object;
    }
}