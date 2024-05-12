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
// File created : 2024, 05, 07 01:05

#endregion

namespace Vvr.MPC.Provider
{
    public interface IEventTarget
    {
        /// <summary>
        /// Server level unique owner id
        /// </summary>
        Owner Owner { get; }
        string DisplayName { get; }
        bool   Disposed    { get; }
    }

    public static class EventTargetExtensions
    {
        public static Hash GetHash(this IEventTarget o)
        {
            return new Hash(unchecked((uint)o.GetHashCode()));
            // Hash hash;
            // if (o is string str) hash                 = new Hash(str);
            // else if (o is UnityEngine.Object uo) hash = new Hash(unchecked((uint)uo.GetInstanceID()));
            // else hash                                 = new Hash(unchecked((uint)o.GetHashCode()));
            // return hash;
        }
    }
}