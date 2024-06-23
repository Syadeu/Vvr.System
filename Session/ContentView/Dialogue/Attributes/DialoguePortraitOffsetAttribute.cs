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
// File created : 2024, 06, 01 12:06

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [Serializable]
    [DisplayName("Offset Portrait")]
    class DialoguePortraitOffsetAttribute : IDialogueAttribute,
        IDialoguePreviewAttribute, IDialogueRevertPreviewAttribute
    {
        enum Position : short
        {
            Left,
            Center,
            Right,
        }

        [SerializeField, EnumToggleButtons, HideLabel]
        private Position m_Position;

        [SerializeField] private bool    m_Relative = true;
        [SerializeField] private Vector2 m_Offset;
        [SuffixLabel("seconds")]
        [SerializeField] private float   m_Duration = .25f;

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = true;

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
            switch (m_Position)
            {
                case Position.Left:
                    target = view.LeftPortrait;
                    break;
                case Position.Center:
                    target = view.CenterPortrait;
                    break;
                case Position.Right:
                    target = view.RightPortrait;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return target;
        }
        private async UniTask ExecutionBody(DialogueAttributeContext ctx)
        {
            var target = GetTarget(ctx.viewProvider.View);
            await target.PanAsync(m_Relative, m_Offset, m_Duration);
        }

        public override string ToString()
        {
            return $"Portrait Offset {m_Position} {m_Offset} {m_Duration}s";
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

        private Vector2 PreviewPreviousPan { get; set; }
#endif

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            var target = GetTarget(view);

            PreviewPreviousPan = target.Pan;
            target.PanAsync(m_Relative, m_Offset, -1).Forget();
#endif
        }

        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var target = GetTarget(view);

            target.PanAsync(false, PreviewPreviousPan, -1).Forget();
#endif
        }
    }
}