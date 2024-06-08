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
// File created : 2024, 05, 14 14:05

#endregion

using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents a data provider for actors.
    /// </summary>
    /// <remarks>
    /// This interface provides access to actor data, including information about actor types, population,
    /// stats, passive abilities, skills, and assets.
    /// </remarks>
    [PublicAPI, LocalProvider]
    public interface IActorDataProvider : IProvider,
        IReadOnlyList<IActorData>
    {
        // public ActorSheet DataSheet { get; }

        /// <summary>
        /// Resolves the actor data with the specified key.
        /// </summary>
        /// <param name="key">The key used to locate the actor data.</param>
        /// <returns>The actor data with the specified key.</returns>
        [NotNull] IActorData Resolve([NotNull] string key);
    }
}