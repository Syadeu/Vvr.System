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
// File created : 2024, 05, 27 09:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents a dialogue attribute that waits for the dialogue view to be fully opened.
    /// </summary>
    [DisplayName("Wait for opened")]
    [Serializable]
    [InfoBox(
        "Because dialogue opens with concurrency, " +
        "with transition. You can ensure attributes fully opened with this.")]
    class DialogueWaitForOpened : IDialogueAttribute
    {
        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            while (!ctx.viewProvider.IsFullyOpened)
            {
                await UniTask.Yield();
            }
        }

        public override string ToString()
        {
            return "Wait for opened";
        }
    }
}