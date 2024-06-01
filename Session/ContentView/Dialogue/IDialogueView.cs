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
// File created : 2024, 05, 26 15:05

#endregion

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents a dialogue view.
    /// </summary>
    public interface IDialogueView : IDialogueViewComponent
    {
        /// <summary>
        /// Represents a background image component for a dialogue view.
        /// </summary>
        IDialogueViewBackground Background { get; }

        /// <summary>
        /// Represents the left portrait component for a dialogue view.
        /// </summary>
        /// <remarks>
        /// This component represents the portrait displayed on the left side of the dialogue view.
        /// </remarks>
        /// <seealso cref="IDialogueViewPortrait"/>
        IDialogueViewPortrait LeftPortrait { get; }

        /// <summary>
        /// Represents a dialogue view for the right portrait.
        /// </summary>
        /// <remarks>
        /// This interface is implemented by classes that represent the right portrait component in a dialogue view.
        /// The right portrait is typically displayed alongside the text in a dialogue scene.
        /// </remarks>
        IDialogueViewPortrait RightPortrait { get; }

        /// <summary>
        /// Represents a component of a dialogue view that displays text.
        /// </summary>
        /// <remarks>
        /// This property is used to access the text component of a dialogue view. It provides methods for clearing the text, skipping the text animation, setting the text content asynchronously, and appending text to the existing content.
        /// </remarks>
        IDialogueViewText        Text { get; }

        /// <summary>
        /// Represents the overlay text component of a dialogue view.
        /// </summary>
        IDialogueViewOverlayText OverlayText { get; }
    }

    /// <summary>
    /// Represents a background image component for a dialogue view.
    /// </summary>
    public interface IDialogueViewBackground : IDialogueViewImageComponent
    {
    }
}