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
// File created : 2024, 05, 26 10:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Provides methods to play dialogues.
    /// </summary>
    [LocalProvider]
    [PublicAPI]
    public interface IDialoguePlayProvider : IProvider
    {
        /// <summary>
        /// Plays a dialogue.
        /// </summary>
        /// <param name="dialogue">The dialogue data to play.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask Play(IDialogueData dialogue);

        /// <summary>
        /// Plays a dialogue.
        /// </summary>
        /// <param name="dialogueAssetPath">The path to the dialogue asset to play.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        UniTask Play(string        dialogueAssetPath);
    }
}