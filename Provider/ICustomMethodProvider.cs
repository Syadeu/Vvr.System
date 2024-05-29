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
// File created : 2024, 05, 16 11:05

#endregion

using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents a delegate for a custom method.
    /// </summary>
    /// <param name="argumentResolver">The argument resolver for resolving method arguments.</param>
    /// <returns>The result of the custom method.</returns>
    public delegate float CustomMethodDelegate(IMethodArgumentResolver argumentResolver);

    /// <summary>
    /// Interface for providing custom methods.
    /// </summary>
    [LocalProvider]
    public interface ICustomMethodProvider : IProvider
    {
        /// <summary>
        /// Represents an indexer that provides access to custom methods.
        /// </summary>
        /// <param name="method">The name of the custom method to access.</param>
        /// <returns>The delegate representing the custom method.</returns>
        /// <remarks>
        /// The custom method delegate is used to invoke a custom method with an argument resolver and return the result.
        /// </remarks>
        CustomMethodDelegate this[CustomMethodNames method] { get; }
    }
}