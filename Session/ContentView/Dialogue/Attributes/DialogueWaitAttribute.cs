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
// File created : 2024, 05, 26 18:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents an attribute that indicates a wait time in a dialogue.
    /// </summary>
    [Serializable]
    [DisplayName("Wait")]
    internal sealed class DialogueWaitAttribute : IDialogueAttribute, IDialogueSkipAttribute
    {
        [SerializeField] private float m_Time = 1;

        async UniTask IDialogueAttribute.ExecuteAsync(
            IDialogue                   dialogue, IAssetProvider assetProvider,
            IDialogueViewProvider           viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            await UniTask.WaitForSeconds(m_Time);
        }

        public override string ToString()
        {
            return $"Wait: {m_Time}";
        }

        public bool CanSkip            => true;
        public bool ShouldWaitForInput => false;

        public UniTask OnSkip(IDialogue        dialogue, IAssetProvider assetProvider, IDialogueViewProvider viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            return UniTask.CompletedTask;
        }
    }
}