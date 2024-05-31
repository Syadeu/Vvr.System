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
// File created : 2024, 06, 01 00:06

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [Serializable]
    [DisplayName("Set Portrait Color")]
    internal sealed class DialogueSetPortraitColorAttribute : IDialogueAttribute, IDialoguePreviewAttribute
    {
        [SerializeField] private bool  m_Right;
        [SerializeField] private Color m_Color    = Color.white;
        [SerializeField] private float m_Duration = .25f;

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = false;

        async UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            if (m_WaitForCompletion)
                await ExecutionBody(ctx);
            else
                ctx.dialogue.RegisterTask(ExecutionBody(ctx));
        }

        private IDialogueViewPortrait GetTarget(in IDialogueView view)
        {
            IDialogueViewPortrait target;
            if (m_Right)
                target  = view.RightPortrait;
            else target = view.LeftPortrait;

            return target;
        }
        private async UniTask ExecutionBody(DialogueAttributeContext ctx)
        {
            IDialogueViewPortrait target = GetTarget(ctx.viewProvider.View);

            await target.SetColorAsync(m_Color, m_Duration);
        }

#if UNITY_EDITOR

        [Button(name: "Normal", DirtyOnClick = true), ButtonGroup]
        private void ColorNormal() => m_Color = Color.white;
        [Button(name: "Disable", DirtyOnClick = true), ButtonGroup]
        private void ColorDisable() => m_Color = new Color(0.7f, 0.7f, 0.7f, 1);

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
            string postfix = m_Right ? " Right" : " Left";
            return "Set Portrait Color" + postfix;
        }

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            IDialogueViewPortrait target = GetTarget(view);

            target.SetColorAsync(m_Color, -1).Forget();
#endif
        }
    }
}