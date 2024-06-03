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
// File created : 2024, 05, 15 02:05

#endregion

using System;
using JetBrains.Annotations;

namespace Vvr.Provider
{
    /// <summary>
    /// Local provider attribute used to mark an interface as a local provider.
    /// </summary>
    /// <remarks>
    /// Local providers are interfaces that are specific to a particular module or component and are not intended to be used as a global provider.
    /// They are usually implemented by classes within the same module or component.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface)]
    [BaseTypeRequired(typeof(IProvider))]
    public sealed class LocalProviderAttribute : Attribute
    {

    }
}