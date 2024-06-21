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
// File created : 2024, 05, 17 04:05

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;

namespace Vvr.Session.Provider
{
    /// <summary>
    /// Interface for providing stage actor instances.
    /// </summary>
    [PublicAPI, LocalProvider]
    public interface IStageActorProvider : IProvider
    {
        [CanBeNull]
        IStageActor Get([NotNull] IActor actor);
        /// <summary>
        /// Creates a stage actor instance by initializing and configuring it with the provided actor and data.
        /// </summary>
        /// <param name="actor">The actor to create a stage actor from.</param>
        /// <param name="data">The data used to configure the stage actor.</param>
        /// <returns>The created stage actor instance.</returns>
        [PublicAPI, NotNull]
        IStageActor Create([NotNull] IActor actor, [NotNull] IActorData data);

        /// <summary>
        /// Reserves a stage actor for reuse by disconnecting and clearing its connections and resetting its state.
        /// </summary>
        /// <param name="item">The stage actor to reserve.</param>
        [PublicAPI]
        void Reserve([NotNull] IStageActor item);
    }
}