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
// File created : 2024, 05, 10 16:05

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Provider
{
    /// <summary>
    /// Represents an actor provider.
    /// </summary>
    [LocalProvider]
    public interface IActorProvider : IProvider
    {
        /// <summary>
        /// Resolves an actor based on the given data.
        /// </summary>
        /// <remarks>
        /// Due to resolved actor is concrete actor should not have any view targets.
        /// </remarks>
        /// <param name="data">The data used to resolve the actor.</param>
        /// <returns>The resolved actor.</returns>
        [PublicAPI, NotNull]
        IReadOnlyActor Resolve([NotNull] IActorData data);

        [PublicAPI, NotNull]
        IActor Create(Owner owner, [NotNull] IActorData data);
    }
}