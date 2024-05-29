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

using JetBrains.Annotations;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents a view component for a dialogue.
    /// </summary>
    [PublicAPI]
    public interface IDialogueViewComponent
    {
        /// <summary>
        /// Gets the transformation component of the game object.
        /// </summary>
        /// <value>
        /// The transformation component of the game object.
        /// </value>
        RectTransform Transform { get; }
    }

    /// <summary>
    /// Contains extension methods for the DialogueViewComponent interface.
    /// </summary>
    public static class DialogueViewComponentExtensions
    {
        /// <summary>
        /// Makes the dialogue view component occupy the entire screen.
        /// </summary>
        /// <param name="c">The dialogue view component.</param>
        /// <returns>The same dialogue view component.</returns>
        public static IDialogueViewComponent FullScreen(this IDialogueViewComponent c)
        {
            c.Transform.localScale       = Vector3.one;
            c.Transform.anchoredPosition = Vector2.zero;
            c.Transform.sizeDelta        = Vector2.zero;
            c.Transform.anchorMin        = Vector2.zero;
            c.Transform.anchorMax        = Vector2.one;

            return c;
        }
    }
}