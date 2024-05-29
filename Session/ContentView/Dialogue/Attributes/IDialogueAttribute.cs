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
        /// <param name="dialogue">The dialogue data.</param>
        /// <param name="assetProvider">The asset provider.</param>
        /// <param name="viewProvider">The dialogue view provider.</param>
        /// <param name="resolveProvider">The dialogue provider resolve delegate.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        UniTask ExecuteAsync(
            [NotNull] IDialogue                       dialogue,
            [NotNull] IAssetProvider                  assetProvider,
            [NotNull] IDialogueViewProvider           viewProvider,
            [NotNull] DialogueProviderResolveDelegate resolveProvider);
    }

    /// <summary>
    /// Represents an attribute that indicates whether the dialogue can be skipped and if it should wait for input.
    /// </summary>
    [PublicAPI]
    public interface IDialogueSkipAttribute
    {
        /// <summary>
        /// Represents an attribute that indicates whether the dialogue can be skipped and if it should wait for input.
        /// </summary>
        /// <remarks>
        /// This attribute is used in dialogue views to determine if the dialogue can be skipped. If the <see cref="CanSkip"/> property
        /// is set to true, the dialogue can be skipped. If the <see cref="ShouldWaitForInput"/> property is set to true,
        /// the dialogue should wait for user input before continuing.
        /// </remarks>
        bool CanSkip            { get; }

        /// <summary>
        /// Gets a value indicating whether the dialogue should wait for user input before continuing.
        /// </summary>
        /// <remarks>
        /// This property is used in dialogue views to determine if the dialogue should wait for user input before continuing.
        /// If the value is true, the dialogue should wait for input. If the value is false, the dialogue can proceed without waiting.
        /// </remarks>
        bool ShouldWaitForInput { get; }

        /// <summary>
        /// Represents a method that is called when the dialogue is being skipped.
        /// </summary>
        /// <param name="dialogue">The dialogue data.</param>
        /// <param name="assetProvider">The asset provider.</param>
        /// <param name="viewProvider">The dialogue view provider.</param>
        /// <param name="resolveProvider">The dialogue provider resolve delegate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask OnSkip(
            [NotNull] IDialogue                       dialogue,
            [NotNull] IAssetProvider                  assetProvider,
            [NotNull] IDialogueViewProvider           viewProvider,
            [NotNull] DialogueProviderResolveDelegate resolveProvider);
    }
}