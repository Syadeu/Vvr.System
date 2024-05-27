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
using Vvr.Provider;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.WorldBackground
{
    [Serializable]
    [DisplayName("Set World Background")]
    class DialogueSetWorldBackgroundAttribute : IDialogueAttribute
    {
        [SerializeField] private string                         m_BackgroundID = "0";
        [SerializeField] private DialogueAssetReference<Sprite> m_Image;

        public async UniTask ExecuteAsync(
            IDialogueData                   dialogue, IAssetProvider assetProvider,
            IDialogueViewProvider           viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            IWorldBackgroundViewProvider v =
                resolveProvider(VvrTypeHelper.TypeOf<IWorldBackgroundViewProvider>.Type) as IWorldBackgroundViewProvider;
            Assert.IsNotNull(v, "v != null");

            var img = await assetProvider.LoadAsync<Sprite>(m_Image.FullPath);

            var view = v.GetView(m_BackgroundID);
            if (view == null)
            {
                v.OpenAsync(assetProvider, m_BackgroundID);
                view = v.GetView(m_BackgroundID);
            }

            await view.SetBackgroundAsync(img.Object);
        }

        public override string ToString()
        {
            string assetName = m_Image.EditorAsset is null ? "None" : m_Image.EditorAsset.name;

            return $"Open World Background: {m_BackgroundID}({assetName})";
        }
    }
}