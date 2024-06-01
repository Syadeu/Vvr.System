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
// File created : 2024, 06, 01 11:06

#endregion

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents a dialogue view for a portrait.
    /// </summary>
    public interface IDialogueViewPortrait : IDialogueViewImageComponent
    {
        /// <summary>
        /// Gets a value indicating whether the dialogue view was in.
        /// </summary>
        /// <value><c>true</c> if the dialogue view was in; otherwise, <c>false</c>.</value>
        bool WasIn { get; }

        Vector2 Pan { get; }

        /// <summary>
        /// Clears the dialogue view portrait.
        /// </summary>
        void Clear();

        /// <summary>
        /// Sets up the dialogue view with the specified portrait and speaker.
        /// </summary>
        /// <param name="portrait">The sprite for the portrait.</param>
        /// <param name="speaker">The dialogue speaker portrait.</param>
        void    Setup(Sprite            portrait, DialogueSpeakerPortrait speaker);

        /// <summary>
        /// Cross fades the dialogue view portrait to a new sprite and its corresponding speaker, and waits for the animation to complete.
        /// </summary>
        /// <param name="sprite">The new sprite to be cross faded to.</param>
        /// <param name="speaker">The corresponding speaker for the new sprite.</param>
        /// <param name="duration">The duration of the cross fade animation in seconds.</param>
        /// <returns>A <see cref="UniTask"/> that represents the asynchronous operation.</returns>
        UniTask CrossFadeAndWait(Sprite sprite, DialogueSpeakerPortrait speaker, Color color, float duration);

        /// <summary>
        /// Fades in the dialogue view portrait with the specified offset and duration, and waits for the fade-in to complete.
        /// </summary>
        /// <param name="offset">The offset to apply to the dialogue view portrait.</param>
        /// <param name="duration">The duration of the fade-in animation.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous fade-in operation.</returns>
        UniTask FadeInAndWait(Vector2 offset, Color color, float duration);

        /// <summary>
        /// Fades out the dialogue view portrait with the specified offset and duration and waits for the fade out to complete.
        /// </summary>
        /// <param name="offset">The offset to move the portrait during the fade out.</param>
        /// <param name="duration">The duration of the fade out animation.</param>
        /// <returns>A Unity Task that represents the asynchronous operation.</returns>
        UniTask FadeOutAndWait(Vector2  offset,   float                   duration);

        UniTask PanAsync(bool relative, Vector2 offset, float duration);
    }
}