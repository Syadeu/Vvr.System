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
// File created : 2024, 05, 27 13:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents an attribute for overlaying text in a dialogue.
    /// </summary>
    [Serializable]
    [DisplayName("Overlay Text")]
    internal sealed class DialogueOverlayTextAttribute : IDialogueAttribute
    {
        [SerializeField, TextArea] private string m_Text;

        [SerializeField] private bool  m_CloseOnEnd    = true;
        [SerializeField] private float m_OpenDuration  = .25f;
        [SerializeField] private float m_Duration      = 2f;
        [SerializeField] private float m_CloseDuration = .25f;

        public async UniTask ExecuteAsync(
            IDialogue                       dialogue, IAssetProvider assetProvider,
            IDialogueViewProvider           viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            IDialogueViewOverlayText overlayText = viewProvider.View.OverlayText;

            await overlayText.OpenAsync(m_OpenDuration);
            await overlayText.SetTextAsync(m_Text);

            await UniTask.WaitForSeconds(m_Duration);

            if (m_CloseOnEnd)
                await overlayText.CloseAsync(m_CloseDuration);
        }

        public override string ToString()
        {
            return m_Text;
        }
    }
}