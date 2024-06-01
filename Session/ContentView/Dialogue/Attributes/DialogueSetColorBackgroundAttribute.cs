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
// File created : 2024, 06, 01 10:06

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [DisplayName("Set Background Color")]
    [Serializable]
    class DialogueSetColorBackgroundAttribute : IDialogueAttribute,
        IDialoguePreviewAttribute, IDialogueRevertPreviewAttribute
    {
        [SerializeField] private Color m_Color    = Color.white;
        [SerializeField] private float m_Duration = .5f;

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = true;

        async UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            if (m_WaitForCompletion)
                await ExecutionBody(ctx);
            else
                ctx.dialogue.RegisterTask(ExecutionBody(ctx));
        }

        private async UniTask ExecutionBody(DialogueAttributeContext ctx)
        {
            await ctx.viewProvider.View.Background.SetColorAsync(m_Color, m_Duration);
        }

        public override string ToString()
        {
            return $"Set Background Color: {m_Color}";
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

        private Color PreviewPreviousColor { get; set; }
#endif
        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            PreviewPreviousColor = view.Background.Image.color;
            view.Background.SetColorAsync(m_Color, -1).Forget();
#endif
        }
        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            view.Background.SetColorAsync(PreviewPreviousColor, -1).Forget();
#endif
        }
    }
}