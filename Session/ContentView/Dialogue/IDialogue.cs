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

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents the interface for a dialogue in a content view session.
    /// </summary>
    [PublicAPI]
    public interface IDialogue : IDialogueData
    {
        /// <summary>
        /// Registers a task for the dialogue.
        /// </summary>
        /// <remarks>Registered task will be waited before close dialogue view.</remarks>
        /// <param name="task">The task to register.</param>
        void RegisterTask(UniTask task);
    }
}