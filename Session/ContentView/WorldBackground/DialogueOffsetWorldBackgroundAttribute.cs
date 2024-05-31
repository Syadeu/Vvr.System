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
// File created : 2024, 06, 01 01:06

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.WorldBackground
{
    [DisplayName("Offset World Background")]
    [UsedImplicitly, Serializable]
    class DialogueOffsetWorldBackgroundAttribute : WorldBackgroundDialogueAttribute,
        IDialoguePreviewAttribute
    {
        [SerializeField] private string  m_BackgroundID = "0";

        [Space] [SerializeField] private float   m_Zoom = 1;
        [SerializeField]         private Vector2 m_Offset;
        [SerializeField]         private bool    m_Relative = false;
        [SerializeField]         private float   m_Duration = .5f;

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = true;

        protected override async UniTask ExecuteAsync(DialogueAttributeContext ctx, IWorldBackgroundViewProvider view)
        {
            if (m_WaitForCompletion)
                await ExecutionBody(ctx, view);
            else
                ctx.dialogue.RegisterTask(ExecutionBody(ctx, view));
        }

        private async UniTask ExecutionBody(DialogueAttributeContext ctx, IWorldBackgroundViewProvider view)
        {
            var targetView = view.GetView(m_BackgroundID);
            if (targetView == null)
            {
                $"Error, view not found {m_BackgroundID}".ToLogError();
                return;
            }

            await UniTask.WhenAll(
                targetView.ZoomAsync(m_Zoom, m_Duration),
                targetView.PanAsync(m_Relative, m_Offset, m_Duration)
            );
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
            string rel = m_Relative ? "Relative" : string.Empty;
            return $"World Background Offset: {m_Relative}({m_Offset})";
        }

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            eView.ZoomAsync(m_Zoom, -1).Forget();
            eView.PanAsync(m_Relative, m_Offset, -1).Forget();
#endif
        }
    }
}