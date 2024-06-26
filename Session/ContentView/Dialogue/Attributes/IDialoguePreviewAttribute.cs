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
// File created : 2024, 05, 31 15:05

#endregion

using JetBrains.Annotations;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// This is an editor-only attribute for previewing dialogue attributes.
    /// </summary>
    /// <remarks>
    /// You can use this attribute by combining <seealso cref="IDialogueRevertPreviewAttribute"/>
    /// </remarks>
    [PublicAPI]
    public interface IDialoguePreviewAttribute
    {
        /// <summary>
        /// Represents a method for previewing a dialogue attribute on a dialogue view.
        /// </summary>
        /// <param name="view">The dialogue view on which the attribute will be previewed.</param>
        void Preview([NotNull] IDialogueView view);
    }
}