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
// File created : 2024, 05, 16 22:05

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Provider;
using Vvr.Session.Actor;

namespace Vvr.Session.Provider
{
    /// <summary>
    /// Represents a timeline queue provider.
    /// </summary>
    [LocalProvider]
    public interface ITimelineQueueProvider : IProvider
    {
        /// <summary>
        /// Gets the number of elements in the timeline queue.
        /// </summary>
        /// <remarks>
        /// This property represents the number of elements currently stored in the timeline queue.
        /// </remarks>
        [PublicAPI]
        int Count { get; }

        /// <summary>
        /// Sets the enabled state of the specified actor in the timeline queue.
        /// </summary>
        /// <param name="actor">The actor whose enabled state needs to be set.</param>
        /// <param name="enabled">Specifies whether the actor should be enabled or disabled.</param>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        void SetEnable([NotNull] IStageActor actor, bool enabled);

        /// <summary>
        /// Finds the index of the specified actor in the timeline queue.
        /// </summary>
        /// <param name="actor">The actor to search for.</param>
        /// <returns>
        /// The index of the specified actor in the timeline queue. If the actor is not found,
        /// -1 is returned.
        /// </returns>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        int IndexOf([NotNull] IStageActor actor);

        /// <summary>
        /// Adds the specified actor to the timeline queue.
        /// </summary>
        /// <param name="actor">The actor to add to the timeline queue.</param>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        void   Enqueue([NotNull] IStageActor actor);

        /// <summary>
        /// Inserts an actor after the specified index in the timeline queue.
        /// </summary>
        /// <param name="index">The index after which to insert the actor.</param>
        /// <param name="actor">The actor to insert.</param>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        void   InsertAfter(int index, [NotNull] IStageActor actor);

        /// <summary>
        /// Removes and returns the first actor in the timeline queue.
        /// </summary>
        /// <returns>
        /// The first actor in the timeline queue.
        /// </returns>
        [PublicAPI]
        IStageActor Dequeue(out float time);

        /// <summary>
        /// Determines whether the given actor is the starting actor in the timeline queue.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>
        /// <c>true</c> if the given actor is the starting actor in the timeline queue; otherwise, <c>false</c>.
        /// </returns>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        bool   IsStartFrom([NotNull] IStageActor actor);

        /// <summary>
        /// Sets the starting point of the timeline queue to the specified actor.
        /// </summary>
        /// <param name="actor">The actor to set as the starting point of the timeline queue.</param>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        void   StartFrom([NotNull] IStageActor actor);

        /// <summary>
        /// Removes the specified actor from the timeline queue.
        /// </summary>
        /// <param name="actor">The actor to be removed from the queue.</param>
        [PublicAPI]
        [ContractAnnotation("actor:null => halt")]
        void Remove([NotNull] IStageActor actor);

        /// <summary>
        /// Clears the timeline queue, removing all actors from it.
        /// </summary>
        [PublicAPI]
        void   Clear();
    }
}