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

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Session.World;

namespace Vvr.Controller.Session
{
    /// <summary>
    /// Represents a parent session.
    /// </summary>
    public interface IParentSession : IGameSessionBase, ISessionTarget
    {
        /// <summary>
        /// Gets the list of child sessions associated with the parent session.
        /// </summary>
        /// <value>
        /// The list of child sessions.
        /// </value>
        IReadOnlyList<IChildSession> ChildSessions { get; }

        /// <summary>
        /// Creates a new child session of type TChildSession.
        /// </summary>
        /// <typeparam name="TChildSession">The type of the child session to create.</typeparam>
        /// <param name="data">The session data for initializing the child session.</param>
        /// <returns>The created child session of type TChildSession.</returns>
        [PublicAPI] [MustUseReturnValue]
        UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data) where TChildSession : IChildSession;
    }
}