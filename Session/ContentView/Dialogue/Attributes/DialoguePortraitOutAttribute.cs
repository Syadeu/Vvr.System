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
        [SerializeField] private bool    m_Right;
        [SerializeField] private Vector2 m_Offset          = new Vector2(100, 0);
        [SerializeField] private float   m_Duration        = .5f;

        [FormerlySerializedAs("m_WaitForComplete")]
        [HideInInspector] [SerializeField]
        private bool m_WaitForCompletion = true;

        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            var target
                = m_Right ? ctx.viewProvider.View.RightPortrait : ctx.viewProvider.View.LeftPortrait;

            Vector2 offset         = m_Offset;
            if (!m_Right) offset.x *= -1f;

            if (m_WaitForCompletion)
                await target.FadeOutAndWait(offset, m_Duration);
            else
            {
                ctx.dialogue.RegisterTask(target.FadeOutAndWait(offset, m_Duration));
            }
        }

        public override string ToString()
        {
            string s = m_Right ? "Right" : "Left";

            return $"Out {s}: {m_Duration}s";
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
            var target = m_Right ? view.RightPortrait : view.LeftPortrait;

            PreviewPreviousImage = target.Image.sprite;

            target.Clear();
#endif
        }
        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var target = m_Right ? view.RightPortrait : view.LeftPortrait;

            target.Image.sprite = PreviewPreviousImage;
#endif
        }
    }
}