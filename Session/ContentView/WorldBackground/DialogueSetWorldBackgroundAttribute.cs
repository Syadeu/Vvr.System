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
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.WorldBackground
{
    [Serializable]
    [DisplayName("Set World Background")]
    class DialogueSetWorldBackgroundAttribute : IDialogueAttribute,
        IDialoguePreviewAttribute
    {
        [SerializeField] private string                         m_BackgroundID = "0";
        [SerializeField] private DialogueAssetReference<Sprite> m_Image;

        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            IWorldBackgroundViewProvider v =
                ctx.resolveProvider(VvrTypeHelper.TypeOf<IWorldBackgroundViewProvider>.Type) as IWorldBackgroundViewProvider;
            Assert.IsNotNull(v, "v != null");

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
        }

        public override string ToString()
        {
            string assetName = m_Image?.EditorAsset is null ? "None" : m_Image.EditorAsset.name;

            return $"Open World Background: {m_BackgroundID}({assetName})";
        }

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            if (m_Image is null || !m_Image.IsValid()) return;

            var eView = WorldBackgroundViewProvider.EditorPreview();
            if (eView is null) return;

            eView.SetBackground(m_Image.EditorAsset);
#endif
        }
    }
}