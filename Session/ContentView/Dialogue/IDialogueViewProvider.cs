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
// File created : 2024, 05, 26 01:05

#endregion

using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.ContentView.Provider;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents a provider for dialogue views.
    /// </summary>
    [LocalProvider, PublicAPI]
    public interface IDialogueViewProvider : IContentViewProvider<DialogueViewEvent>
    {
        /// <summary>
        /// Represents a provider for dialogue views.
        /// </summary>
        IDialogueView View { get; }

        /// <summary>
        /// Gets a value indicating whether the dialogue view is fully opened.
        /// </summary>
        /// <remarks>
        /// The value of this property determines whether the dialogue view has completed its opening animation and is fully visible to the user.
        /// </remarks>
        bool IsFullyOpened { get; }
    }
}