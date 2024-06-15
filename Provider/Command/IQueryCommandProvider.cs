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
// File created : 2024, 06, 15 20:06

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Provider.Command
{
    /// <summary>
    /// Represents a provider that can enqueue query commands.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    [PublicAPI, RequireImplementors]
    [LocalProvider]
    public interface IQueryCommandProvider<TQuery> : IProvider
        where TQuery : unmanaged
    {
        UniTask WaitForQueryFlush { get; }

        /// <summary>
        /// Enqueues a query command for execution.
        /// </summary>
        /// <typeparam name="TCommand">The type of the query command.</typeparam>
        /// <param name="command">The query command to enqueue.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="command"/> is null.</exception>
        /// <remarks>
        /// The query command must implement the <see cref="IQueryCommand{TQuery}"/> interface, where <typeparamref name="TQuery"/>
        /// is the type of query that the command can execute. The command is added to a queue for later execution.
        /// </remarks>
        void Enqueue<TCommand>(TCommand command) where TCommand : IQueryCommand<TQuery>;
    }
}