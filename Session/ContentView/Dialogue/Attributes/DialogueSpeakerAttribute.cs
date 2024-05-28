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
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [Serializable]
    [DisplayName("Speaker")]
    public class DialogueSpeakerAttribute : IDialogueAttribute, IDialogueSkipAttribute
    {
        [SerializeField] private string m_DisplayName;
        [SerializeField] private float  m_Time;

        [Space] [SerializeField]   private TextAlignmentOptions m_Alignment = TextAlignmentOptions.TopLeft;
        [SerializeField, TextArea] private string               m_Message;

        async UniTask IDialogueAttribute.ExecuteAsync(IDialogueData dialogue, IAssetProvider assetProvider, IDialogueViewProvider viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            $"[Dialogue] Speak".ToLog();

            var view = viewProvider.View;

            if (m_Message.IsNullOrEmpty())
                view.Text.Clear();
            else
            {
                view.Text.Text.alignment = m_Alignment;
                await view.Text.SetTextAsync(m_DisplayName, m_Message);
            }

            await UniTask.WaitForSeconds(m_Time);
        }

        public override string ToString()
        {
            return $"{m_DisplayName}: {m_Message}";
        }

        public bool CanSkip            => true;
        public bool ShouldWaitForInput => true;

        public async UniTask OnSkip(IDialogueData        dialogue, IAssetProvider assetProvider, IDialogueViewProvider viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            viewProvider.View.Text.SkipText();
        }
    }
}