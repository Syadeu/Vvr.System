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
// File created : 2024, 05, 21 20:05

#endregion

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Vvr.Session.ContentView.Dialogue
{
    [CreateAssetMenu(menuName = "Vvr/Create DialogueSpeakerPortrait", fileName = "DialogueSpeakerPortrait", order = 0)]
    [HideMonoScript]
    public class DialogueSpeakerPortrait : ScriptableObject
    {
        [SerializeField] private AssetReferenceSprite m_Portrait;

#if UNITY_EDITOR
        [ShowInInspector, PreviewField(200), HideLabel]
        private Sprite Preview => (Sprite)Portrait.editorAsset;
#endif

        [Space]
        [SerializeField] private Vector3 m_PositionOffset;
        [SerializeField] private Vector3 m_Rotation;
        [SerializeField] private Vector3 m_Scale = Vector3.one;

        public Vector3 PositionOffset
        {
            get => m_PositionOffset;
            set => m_PositionOffset = value;
        }

        public Vector3 Rotation
        {
            get => m_Rotation;
            set => m_Rotation = value;
        }

        public Vector3 Scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        public AssetReferenceSprite Portrait => m_Portrait;
    }
}