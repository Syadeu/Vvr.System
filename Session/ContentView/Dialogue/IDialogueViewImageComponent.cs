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
// File created : 2024, 05, 26 16:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents a view component for an image in a dialogue.
    /// </summary>
    [PublicAPI]
    public interface IDialogueViewImageComponent : IDialogueViewComponent
    {
        /// <summary>
        /// Represents a view component for an image in a dialogue.
        /// </summary>
        /// <remarks>
        /// This interface is used to define a view component for displaying images in a dialogue.
        /// It provides properties and methods for manipulating the image component, such as setting the color and crossfading to a new sprite.
        /// The interface inherits from the <see cref="IDialogueViewComponent"/> interface, which represents a base component for a dialogue view.
        /// </remarks>
        Image   Image { get; }

        /// <summary>
        /// Sets the color of the image component with a given duration.
        /// </summary>
        /// <param name="color">The color to set the image component.</param>
        /// <param name="duration">The duration in which the color transition should happen.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        UniTask SetColorAsync(Color   color,  float duration);

        /// <summary>
        /// Cross-fades the image of a dialogue view component with a given duration and waits for the transition to complete.
        /// </summary>
        /// <param name="sprite">The sprite to fade to.</param>
        /// <param name="color">The color to fade to.</param>
        /// <param name="duration">The duration in which the fade transition should happen.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        UniTask CrossFadeAndWaitAsync(Sprite sprite, Color color, float duration);
    }
}