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

using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Provider.Command
{
    /// <summary>
    /// Represents a query command that can be executed on a specific type of query.
    /// </summary>
    /// <typeparam name="TQuery">The type of query to execute.</typeparam>
    /// <remarks>
    /// This interface should be implemented by command classes that execute queries on a specific type of query.
    /// The queries must be of type <typeparamref name="TQuery"/>.
    /// </remarks>
    [PublicAPI, RequireImplementors]
    public interface IQueryCommand<TQuery> where TQuery : unmanaged
    {
        /// <summary>
        /// Executes a query command on a specific type of query.
        /// </summary>
        /// <typeparam name="TQuery">The type of query to execute.</typeparam>
        /// <param name="query">The query object to execute.</param>
        /// <remarks>
        /// This method executes the query command defined by the <typeparamref name="TQuery"/> type on the specified query object.
        /// The query command should implement the <see cref="IQueryCommand{TQuery}"/> interface and define the <see cref="IQueryCommand{TQuery}.Execute(ref TQuery)"/> method.
        /// </remarks>
        void Execute(ref TQuery query);
    }
}