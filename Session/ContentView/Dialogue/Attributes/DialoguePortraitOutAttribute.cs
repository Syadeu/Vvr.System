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
using UnityEngine;
using UnityEngine.Serialization;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents a dialogue attribute for fading out a portrait in a dialogue view.
    /// </summary>
    [Serializable]
    [DisplayName("Portrait Out")]
    internal sealed class DialoguePortraitOutAttribute : IDialogueAttribute,
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
        [SerializeField] private Vector2 m_Offset          = new Vector2(100, 0);
        [SerializeField] private float   m_Duration        = .5f;

        [FormerlySerializedAs("m_WaitForComplete")]
        [HideInInspector] [SerializeField]
        private bool m_WaitForCompletion = true;

        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            var target = GetTarget(ctx.viewProvider.View);

            Vector2 offset                                = m_Offset;
            if (m_Position == Position.Left) offset.x *= -1f;

            if (m_WaitForCompletion)
                await target.FadeOutAndWait(offset, m_Duration);
            else
            {
                ctx.dialogue.RegisterTask(target.FadeOutAndWait(offset, m_Duration));
            }
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

        public override string ToString()
        {
            return $"Out {m_Position}: {m_Duration}s";
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

        private Sprite PreviewPreviousImage { get; set; }
#endif
        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            var target = GetTarget(view);

            PreviewPreviousImage = target.Image.sprite;

            target.Clear();
#endif
        }
        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var target = GetTarget(view);

            target.Image.sprite = PreviewPreviousImage;
#endif
        }
    }
}