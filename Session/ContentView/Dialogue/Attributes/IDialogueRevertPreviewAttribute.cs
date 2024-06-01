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

using JetBrains.Annotations;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// This is editor only attribute that can be reverted after preview.
    /// If target attribute has no <see cref="IDialoguePreviewAttribute"/>,
    /// behavior most likely unexpected results
    /// </summary>
    [PublicAPI]
    public interface IDialogueRevertPreviewAttribute
    {
        /// <summary>
        /// Reverts the changes made by a dialogue attribute on a dialogue view.
        /// </summary>
        /// <param name="view">The dialogue view to revert the changes on.</param>
        void Revert([NotNull] IDialogueView view);
    }
}