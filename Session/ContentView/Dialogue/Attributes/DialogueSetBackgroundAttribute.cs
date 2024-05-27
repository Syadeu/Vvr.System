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
// File created : 2024, 05, 27 09:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [DisplayName("Set Background")]
    [Serializable]
    class DialogueSetBackgroundAttribute : IDialogueAttribute
    {
        [SerializeField] private DialogueAssetReference<Sprite>
            m_Image;

        [SerializeField] private float m_Duration = .5f;

        public async UniTask ExecuteAsync(IDialogueData dialogue, IAssetProvider assetProvider, IDialogueViewProvider viewProvider,
            DialogueProviderResolveDelegate resolveProvider)
        {
            var sprite = await assetProvider.LoadAsync<Sprite>(m_Image.FullPath);

            await viewProvider.View.Background.CrossFadeAndWait(sprite?.Object, m_Duration);
        }

        public override string ToString()
        {
            if (m_Image.EditorAsset == null)
            {
                return "None";
            }

            return m_Image.EditorAsset.name;
        }
    }
}