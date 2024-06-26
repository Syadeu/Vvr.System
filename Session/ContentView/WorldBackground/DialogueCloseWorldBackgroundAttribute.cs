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
// File created : 2024, 05, 27 15:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Vvr.Provider;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.WorldBackground
{
    [Serializable]
    [DisplayName("Close World Background")]
    class DialogueCloseWorldBackgroundAttribute : IDialogueAttribute,
        IDialoguePreviewAttribute, IDialogueRevertPreviewAttribute
    {
        [SerializeField] private string m_BackgroundID = "0";

        [SuffixLabel("seconds")]
        [SerializeField] private float m_Delay = 0;

        [HideInInspector]
        [FormerlySerializedAs("m_WaitForClose")]
        [SerializeField] private bool   m_WaitForCompletion = true;

        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            if (m_WaitForCompletion)
                await ExecutionBody(ctx);
            else
            {
                ctx.dialogue.RegisterTask(ExecutionBody(ctx));
            }
        }

        private async UniTask ExecutionBody(DialogueAttributeContext ctx)
        {
            IWorldBackgroundViewProvider v =
                ctx.resolveProvider(VvrTypeHelper.TypeOf<IWorldBackgroundViewProvider>
                    .Type) as IWorldBackgroundViewProvider;
            Assert.IsNotNull(v, "v != null");

            if (v.GetView(m_BackgroundID) == null) return;

            if (m_Delay > 0) await UniTask.WaitForSeconds(m_Delay);

            await v.CloseAsync(m_BackgroundID, ctx.cancellationToken);
        }

        public override string ToString()
        {
            return $"Close World Background: {m_BackgroundID}";
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
            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            PreviewPreviousImage = eView.Image.sprite;
            eView.SetBackground(null);
#endif
        }

        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            eView.SetBackground(PreviewPreviousImage);
#endif
        }
    }
}