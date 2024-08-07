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
// File created : 2024, 05, 23 01:05

#endregion

using JetBrains.Annotations;

namespace Vvr.Model
{
    /// <summary>
    /// Represents a method argument resolver.
    /// </summary>
    [PublicAPI]
    public interface IMethodArgumentResolver
    {
        /// <summary>
        /// Resolves the value of a method argument.
        /// </summary>
        /// <param name="arg">The argument name to resolve the value for.</param>
        /// <returns>The resolved value of the argument.</returns>
        /// <exception cref="System.ObjectDisposedException">Thrown if the research node is disposed.</exception>
        /// <exception cref="System.ArgumentException">Thrown if the provided argument name is not valid.</exception>
        /// <remarks>
        /// This method resolves the value of a method argument based on the provided argument name.
        /// </remarks>
        [PublicAPI]
        float Resolve(string arg);
    }
}