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
// File created : 2024, 06, 01 20:06

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.WorldBackground
{
    [Serializable]
    [DisplayName("Color World Background")]
    class DialogueColorWorldBackgroundAttribute : WorldBackgroundDialogueAttribute,
        IDialoguePreviewAttribute, IDialogueRevertPreviewAttribute
    {
        [SerializeField] private string m_BackgroundID = "0";

        [SerializeField] private Color m_Color = Color.white;
        [SuffixLabel("seconds")]
        [SerializeField] private float m_Duration = .25f;

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = true;

        protected override async UniTask ExecuteAsync(DialogueAttributeContext ctx, IWorldBackgroundViewProvider view)
        {
            if (m_WaitForCompletion)
                await ExecutionBody(ctx, view);
            else
                ctx.dialogue.RegisterTask(ExecutionBody(ctx, view));
        }

        protected async UniTask ExecutionBody(DialogueAttributeContext ctx, IWorldBackgroundViewProvider view)
        {
            var target = view.GetView(m_BackgroundID);
            if (target is null)
            {
                $"Error, view not found {m_BackgroundID}".ToLogError();
                return;
            }

            await target.SetColorAsync(m_Color, m_Duration);
        }

        public override string ToString()
        {
            return "Color World Background";
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
            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            PreviewPreviousColor = eView.Image.color;

            eView.SetColorAsync(m_Color, -1).Forget();
#endif
        }
        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            eView.SetColorAsync(PreviewPreviousColor, -1).Forget();
#endif
        }
    }
}