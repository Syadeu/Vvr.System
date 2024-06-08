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
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a parent session.
    /// </summary>
    public interface IParentSession : IGameSessionBase, ISessionTarget, IDependencyContainer
    {
        Type Type { get; }

        /// <summary>
        /// Gets the list of child sessions associated with the parent session.
        /// </summary>
        /// <value>
        /// The list of child sessions.
        /// </value>
        [PublicAPI]
        IReadOnlyList<IChildSession> ChildSessions { get; }

        /// <summary>
        /// Creates a new child session of type TChildSession on a background thread.
        /// </summary>
        /// <typeparam name="TChildSession">The type of the child session to create.</typeparam>
        /// <param name="data">The session data for initializing the child session.</param>
        /// <returns>The created child session of type TChildSession.</returns>
        /// <remarks>
        /// The method creates the child session of type TChildSession by invoking the <see cref="CreateSession{TChildSession}"/> method in a background thread.
        /// </remarks>
        [PublicAPI]
        UniTask<TChildSession> CreateSessionOnBackground<TChildSession>([CanBeNull] ISessionData data)
            where TChildSession : IChildSession, new();

        /// <summary>
        /// Creates a new child session of type TChildSession.
        /// </summary>
        /// <typeparam name="TChildSession">The type of the child session to create.</typeparam>
        /// <param name="data">The session data for initializing the child session.</param>
        /// <returns>The created child session of type TChildSession.</returns>
        [PublicAPI]
        UniTask<TChildSession> CreateSession<TChildSession>([CanBeNull] ISessionData data)
            where TChildSession : IChildSession, new();

        /// <summary>
        /// Waits until a session of the specified sessionType becomes available in the IParentSession.
        /// </summary>
        /// <param name="sessionType">The type of the session to wait for.</param>
        /// <param name="timeout">The maximum time to wait in seconds. Defaults to 10 seconds.</param>
        /// <returns>A UniTask representing the asynchronous wait operation, which completes when the session becomes available.</returns>
        /// <remarks>
        /// The method waits until a session of the specified sessionType becomes available in the IParentSession. If a session of the specified sessionType is already available, the method returns immediately. Otherwise, it keeps checking for the availability of the session in the background, until a session of the specified sessionType is found or the specified timeout is reached. Once the session becomes available, the UniTask is completed and the found session is returned.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if sessionType is null.</exception>
        [PublicAPI]
        [ContractAnnotation("sessionType:null => halt")]
        UniTask<IChildSession> WaitUntilSessionAvailableAsync([NotNull] Type sessionType, float timeout = 10);

        /// <summary>
        /// Waits until a session of the specified Type becomes available.
        /// </summary>
        /// <typeparam name="TChildSession">The type of the child session to wait for.</typeparam>
        /// <param name="timeout">The timeout duration in seconds (optional, default is 10 seconds).</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        /// <remarks>
        /// The method waits until a session of type TChildSession becomes available by invoking the appropriate session method. If the session does not become available within the specified timeout duration, an exception is thrown.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when sessionType is null.</exception>
        [PublicAPI]
        UniTask<TChildSession> WaitUntilSessionAvailableAsync<TChildSession>(float timeout = 10)
            where TChildSession : class, IChildSession;

        /// <summary>
        /// Gets the child session of the specified session type.
        /// </summary>
        /// <param name="sessionType">The session type.</param>
        /// <returns>The child session of the specified session type, or null if not found.</returns>
        [PublicAPI, MustUseReturnValue]
        [ContractAnnotation("sessionType:null => halt")]
        IChildSession GetSession([NotNull] Type sessionType);

        /// <summary>
        /// Gets the child session of the specified session type.
        /// </summary>
        /// <typeparam name="TChildSession">The type of the child session to get.</typeparam>
        /// <returns>The child session of type TChildSession, or null if not found.</returns>
        [PublicAPI, MustUseReturnValue]
        TChildSession GetSession<TChildSession>() where TChildSession : class, IChildSession;

        /// <summary>
        /// Closes all child sessions and clears the list of child sessions.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation of closing all child sessions.</returns>
        [PublicAPI]
        UniTask CloseAllSessions();
    }
}