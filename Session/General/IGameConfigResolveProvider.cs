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
// File created : 2024, 05, 21 09:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Interface for resolving game configuration.
    /// </summary>
    [LocalProvider]
    public interface IGameConfigResolveProvider : IProvider
    {
        /// <summary>
        /// Resolves game configuration based on the provided event target, condition, and value.
        /// </summary>
        /// <param name="e">The event target.</param>
        /// <param name="condition">The condition to resolve.</param>
        /// <param name="value">The value used in the resolution.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        [PublicAPI]
        UniTask Resolve(IEventTarget e, Condition condition, string value);
    }
}