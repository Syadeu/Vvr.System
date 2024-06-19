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
// File created : 2024, 06, 20 03:06

#endregion

using System;
using JetBrains.Annotations;

namespace Vvr.Session
{
    /// <summary>
    /// Represents an attribute that marks a class as a provider session.
    /// </summary>
    /// <remarks>
    /// The ProviderSessionAttribute is used to mark a class as a provider session.
    /// If the session has this attribute, creating session will also provide Provider to parent directly.
    /// It should be applied to classes that implement the IChildSession interface.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IChildSession))]
    [PublicAPI]
    public sealed class ProviderSessionAttribute : Attribute
    {
        [CanBeNull] public Type[] ProviderTypes { get; }

        public ProviderSessionAttribute(params Type[] providerTypes)
        {
            ProviderTypes = providerTypes;
        }
    }
}