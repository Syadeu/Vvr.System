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
// File created : 2024, 05, 27 11:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.WorldBackground
{
    [Serializable]
    [DisplayName("Open World Background")]
    class DialogueSetWorldBackgroundAttribute : WorldBackgroundDialogueAttribute,
        IDialoguePreviewAttribute, IDialogueRevertPreviewAttribute
    {
        [SerializeField] private string m_BackgroundID = "0";

#if UNITY_EDITOR
        [InfoBox(
            "Invalid image.", InfoMessageType.Error,
            VisibleIf = nameof(EvaluateImageIsInvalid))]
#endif
        [SerializeField]
        private DialogueAssetReference<Sprite> m_Image = new();

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = true;

        protected override async UniTask ExecuteAsync(DialogueAttributeContext ctx, IWorldBackgroundViewProvider v)
        {
            if (m_WaitForCompletion)
                await ExecutionBody(ctx, v);
            else
                ctx.dialogue.RegisterTask(ExecutionBody(ctx, v));
        }

        private async UniTask ExecutionBody(DialogueAttributeContext ctx, IWorldBackgroundViewProvider v)
        {
            var img = await ctx.assetProvider.LoadAsync<Sprite>(m_Image.FullPath);

            var view = v.GetView(m_BackgroundID);
            if (view == null)
            {
                var canvas = ctx.resolveProvider(VvrTypeHelper.TypeOf<ICanvasViewProvider>.Type) as ICanvasViewProvider;
                v.OpenAsync(canvas, ctx.assetProvider, m_BackgroundID)
                    .Forget();
                while ((view = v.GetView(m_BackgroundID)) == null)
                {
                    await UniTask.Yield();
                }
            }

            view.SetBackground(img.Object);
            view.ZoomAsync(1, -1).Forget();
        }

        public override string ToString()
        {
            string assetName = m_Image?.EditorAsset is null ? "None" : m_Image.EditorAsset.name;

            return $"Open World Background: {m_BackgroundID}({assetName})";
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

        private bool EvaluateImageIsInvalid(DialogueAssetReference<Sprite> value)
        {
            if (value is null || !value.IsValid())
            {
                return true;
            }

            return false;
        }
#endif

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            if (m_Image is null || !m_Image.IsValid())
            {
                "Image is not valid".ToLogError();
                return;
            }

            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            eView.SetBackground(m_Image.EditorAsset);
            eView.ZoomAsync(1, -1).Forget();
#endif
        }
        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            eView.SetBackground(null);
#endif
        }
    }
}