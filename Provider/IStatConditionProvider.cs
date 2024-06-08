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
// File created : 2024, 05, 10 22:05

#endregion

using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents a provider for stat condition resolution.
    /// </summary>
    [LocalProvider]
    public interface IStatConditionProvider : IProvider
    {
        /// <summary>
        /// Represents an indexer for the IStatConditionProvider interface.
        /// </summary>
        /// <param name="t">The string key used to retrieve the StatType value.</param>
        /// <returns>The StatType value associated with the specified key.</returns>
        StatType this[string t] { get; }

        /// <summary>
        /// Resolves a stat condition.
        /// </summary>
        /// <param name="centerStats">The center stats to compare against.</param>
        /// <param name="stats">The stats to compare.</param>
        /// <param name="condition">The operator condition.</param>
        /// <param name="value">The value of the condition.</param>
        /// <returns>True if the condition is resolved successfully, otherwise false.</returns>
        bool Resolve(
            IReadOnlyStatValues centerStats,
            IReadOnlyStatValues stats, OperatorCondition condition, string value);
    }
}