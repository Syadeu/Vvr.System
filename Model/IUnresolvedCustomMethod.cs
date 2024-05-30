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
// File created : 2024, 05, 30 12:05

#endregion

using System;
using JetBrains.Annotations;

namespace Vvr.Model
{
    /// <summary>
    /// Represents an unresolved custom method.
    /// </summary>
    [PublicAPI]
    public interface IUnresolvedCustomMethod
    {
        /// <summary>
        /// Executes the unresolved custom method.
        /// </summary>
        /// <param name="resolver">The method argument resolver.</param>
        /// <returns>The result of executing the unresolved custom method.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there are not enough variables to execute the method.</exception>
        /// <remarks>
        /// This method executes the unresolved custom method by resolving the variables and performing the necessary operations.
        /// If there is only one variable, the method returns its resolved value.
        /// If there are multiple variables and method values, the method resolves the variables and performs the operations based on their method types in the correct order.
        /// </remarks>
        float Execute(IMethodArgumentResolver resolver);
    }
}