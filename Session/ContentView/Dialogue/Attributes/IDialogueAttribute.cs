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
// File created : 2024, 05, 26 17:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents a delegate for resolving a dialogue provider based on the provider type.
    /// </summary>
    /// <param name="providerType">The type of the provider.</param>
    /// <returns>The resolved dialogue provider.</returns>
    /// <remarks>
    /// Usage example:
    /// <code>
    /// IProvider provider = dialogueProviderResolveDelegate(typeof(SomeProvider));
    /// </code>
    /// </remarks>
    [CanBeNull]
    public delegate IProvider DialogueProviderResolveDelegate([NotNull] Type providerType);

    /// <summary>
    /// Represents a dialogue attribute.
    /// </summary>
    [PublicAPI, RequireImplementors]
    public interface IDialogueAttribute
    {
        /// <summary>
        /// Executes the dialogue attribute asynchronously.
        /// </summary>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        UniTask ExecuteAsync(DialogueAttributeContext ctx);
    }
}