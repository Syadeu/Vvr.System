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
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents an attribute for overlaying text in a dialogue.
    /// </summary>
    [Serializable]
    [DisplayName("Show Overlay Text")]
    internal sealed class DialogueShowOverlayTextAttribute : IDialogueAttribute,
        IDialoguePreviewAttribute
    {
        [SerializeField, TextArea] private string m_Text;

        [SerializeField] private float m_Duration      = 2f;
        [SerializeField] private float m_OpenDuration  = .25f;

        [HideInInspector]
        [SerializeField] private bool m_WaitForCompletion = true;

        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            IDialogueViewOverlayText overlayText = ctx.viewProvider.View.OverlayText;

            await overlayText.OpenAsync(m_OpenDuration);
            await overlayText.SetTextAsync(m_Text);

            var task = UniTask.WaitForSeconds(m_Duration);

            if (m_WaitForCompletion)
                await task;
            else
                ctx.dialogue.RegisterTask(task);
        }

#if UNITY_EDITOR
        [ShowIf(nameof(m_WaitForCompletion))]
        [VerticalGroup("0")]
        [Button(ButtonSizes.Medium, DirtyOnClick = true), GUIColor(0, 1, 0)]
        private void WaitForCompletion() => m_WaitForCompletion = false;

        [HideIf(nameof(m_WaitForCompletion))]
        [VerticalGroup("0")]
        [Button(ButtonSizes.Medium, DirtyOnClick = true), GUIColor(1, .2f, 0)]
        private void DontWaitForCompletion() => m_WaitForCompletion = true;
#endif

        public override string ToString()
        {
            return $"Overlay Text: {m_Text}";
        }

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            view.OverlayText.Clear();
            view.OverlayText.OpenAsync(-1);
            view.OverlayText.Text.text = m_Text;
#endif
        }
    }
}