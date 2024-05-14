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
    /// <summary>
    /// Represents an event target.
    /// </summary>
    public interface IEventTarget
    {
        /// <summary>
        /// Server level unique owner id
        /// </summary>
        Owner Owner { get; }

        /// <summary>
        /// Gets the display name of the object.
        /// </summary>
        /// <value>
        /// The display name of the object.
        /// </value>
        string DisplayName { get; }

        /// <summary>
        /// Gets a value indicating whether the object has been disposed.
        /// </summary>
        bool   Disposed    { get; }
    }

    public static class EventTargetExtensions
    {
        public static Hash GetHash(this IEventTarget o)
        {
            return new Hash(unchecked((uint)o.GetHashCode()));
        }
    }
}