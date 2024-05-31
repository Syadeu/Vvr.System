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
// File created : 2024, 05, 29 14:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using TMPro;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents the overlay text component of a dialogue view.
    /// </summary>
    [PublicAPI]
    public interface IDialogueViewOverlayText : IDialogueViewComponent
    {
        TextMeshProUGUI Text { get; }

        /// <summary>
        /// Sets the text of the dialogue overlay asynchronously, animating it if the text is not empty.
        /// </summary>
        /// <param name="text">The text to display in the overlay.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask SetTextAsync(string text);

        /// <summary>
        /// Opens the dialogue overlay asynchronously, animating it if it is not already open.
        /// </summary>
        /// <param name="duration">The duration of the opening animation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask OpenAsync(float duration);

        /// <summary>
        /// Closes the dialogue overlay asynchronously, animating it if it is not already closed.
        /// </summary>
        /// <param name="duration">The duration of the closing animation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask CloseAsync(float duration);

        /// <summary>
        /// Clears the dialogue overlay by stopping the text animation,
        /// clearing the text, setting the alpha to 0, and marking it as not opened.
        /// </summary>
        void Clear();
    }
}