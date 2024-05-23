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
// File created : 2024, 05, 23 11:05

#endregion

using System.Collections.Generic;
using Vvr.Model;

namespace Vvr.Provider
{
    /// <summary>
    /// Cached custom method static container
    /// </summary>
    /// <remarks>
    /// Custom methods are designed for recycling.
    /// We can cache the created methods here to use them throughout the project.
    /// </remarks>
    public struct CustomMethod
    {
        private static readonly LinkedList<UnresolvedCustomMethod>     s_Methods         = new();
        private static readonly Dictionary<uint, CustomMethodDelegate> s_CachedDelegates = new();

        public static CustomMethod Static => default;

        public CustomMethodDelegate this[CustomMethodSheet.Row method]
        {
            get
            {
                uint hash = FNV1a32.Calculate(method.Id);
                if (!s_CachedDelegates.TryGetValue(hash, out var m))
                {
                    var body            = new UnresolvedCustomMethod(method);
                    s_Methods.AddLast(body);

                    s_CachedDelegates[hash] = body.Execute;
                }

                return m;
            }
        }
    }
}