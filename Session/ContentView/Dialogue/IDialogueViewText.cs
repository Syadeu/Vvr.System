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
    /// Represents a component of a dialogue view that displays text.
    /// </summary>
    [PublicAPI]
    public interface IDialogueViewText : IDialogueViewComponent
    {
        TextMeshProUGUI Title { get; }
        /// <summary>
        /// Represents a component of a dialogue view that displays text.
        /// </summary>
        TextMeshProUGUI Text { get; }

        /// <summary>
        /// Clears the dialogue view by resetting the displayed text and hiding the associated components.
        /// </summary>
        void    Clear();

        /// <summary>
        /// Skips the text animation of the dialogue view by calling the SkipTypewriter method of the TypewriterCore component.
        /// </summary>
        void    SkipText();

        /// <summary>
        /// Sets the text of the dialogue view asynchronously.
        /// </summary>
        /// <param name="title">The title of the text.</param>
        /// <param name="text">The content of the text.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        UniTask SetTextAsync(string    title, string text);

        /// <summary>
        /// Appends the provided text to the existing text asynchronously.
        /// </summary>
        /// <param name="text">The text to be appended.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        UniTask AppendTextAsync(string text);
    }
}