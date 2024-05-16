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
// File created : 2024, 05, 10 20:05

#endregion

using System;

namespace Vvr.Session
{
    /// <summary>
    /// Represents an attribute that specifies the parent session for a child session.
    /// </summary>
    /// <seealso cref="ChildSession{TSessionData}"/>
    /// <example>
    /// The following example demonstrates how to use the ParentSessionAttribute:
    /// <code>
    /// [ParentSession(typeof(DefaultWorld), true)]
    /// public partial class DefaultFloor : ParentSession&lt;DefaultFloor.SessionData&gt;, IConnector&lt;IActorProvider&gt;
    /// {
    /// // class implementation
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ParentSessionAttribute : Attribute
    {
        /// <summary>
        /// Represents an attribute that specifies the parent session for a child session.
        /// </summary>
        /// <seealso cref="ChildSession{TSessionData}"/>
        /// <example>
        /// The following example demonstrates how to use the ParentSessionAttribute:
        /// <code>
        /// [ParentSession(typeof(DefaultWorld), true)]
        /// public partial class DefaultFloor : ParentSession&lt;DefaultFloor.SessionData&gt;, IConnector&lt;IActorProvider&gt;
        /// {
        /// // class implementation
        /// }
        /// </code>
        /// </example>
        public Type Type            { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute should include the inheritance check.
        /// </summary>
        /// <remarks>
        /// This property is used to specify whether the ParentSessionAttribute should include the inheritance check when initializing a child session.
        /// If IncludeInherits is set to true, the parent session's type is checked to ensure that it is derived from the specified type in the ParentSessionAttribute.
        /// If IncludeInherits is set to false, the parent session's type must match exactly with the specified type in the ParentSessionAttribute.
        /// </remarks>
        /// <seealso cref="ParentSessionAttribute"/>
        public bool IncludeInherits { get; set; }

        public ParentSessionAttribute(Type t)
        {
            Type = t;
        }

        public ParentSessionAttribute(Type t, bool includeInherits)
        {
            Type            = t;
            IncludeInherits = includeInherits;
        }
    }
}