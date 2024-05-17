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
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a child session.
    /// </summary>
    public interface IChildSession : IGameSessionBase, ISessionTarget
    {
        /// <summary>
        /// Represents the type of the session.
        /// </summary>
        /// <remarks>
        /// The session type is obtained by calling the Type property of the implementing class.
        /// </remarks>
        public Type Type { get; }

        /// <summary>
        /// Represents the root parent session of a child session.
        /// </summary>
        /// <remarks>
        /// The root parent session is the topmost session in the hierarchy that does not have a parent session.
        /// It is obtained by traversing up the parent sessions until a session without a parent is found.
        /// </remarks>
        [PublicAPI]
        IParentSession Root { get; }
        /// <summary>
        /// Represents an interface for a parent session.
        /// </summary>
        [PublicAPI]
        IParentSession Parent { get; }
    }
}