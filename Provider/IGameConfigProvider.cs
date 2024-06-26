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
// File created : 2024, 05, 11 17:05

#endregion

using System;
using System.Collections.Generic;
using Vvr.Model;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents a provider for game configuration data.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by classes that provide game configuration data.
    /// </remarks>
    public interface IGameConfigProvider : IProvider
    {
        /// <summary>
        /// Gets the game configuration data for the specified map type.
        /// </summary>
        /// <param name="t">The map type.</param>
        /// <returns>The game configuration data for the specified map type.</returns>
        IEnumerable<GameConfigSheet.Row> this[MapType t] { get; }
    }
}