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
// File created : 2024, 05, 26 19:05

#endregion

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [Serializable]
    [DisplayName("Portrait In")]
    sealed class DialoguePortraitInAttribute : IDialogueAttribute, ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        const string EditorPrefix = "Assets/AddressableResources/";

        private DialogueSpeakerPortrait m_EditorPortrait;

#endif
        [HideInInspector]
        [SerializeField] private string m_PortraitGuid;

        [HideInInspector]
        [SerializeField] private string m_PortraitFullPath;

#if UNITY_EDITOR
        [ShowInInspector, PropertyOrder(-1)]
        private DialogueSpeakerPortrait Portrait
        {
            get
            {
                if (m_EditorPortrait == null &&
                    !m_PortraitGuid.IsNullOrEmpty())
                {
                    m_EditorPortrait = AssetDatabase.LoadAssetAtPath<DialogueSpeakerPortrait>(
                        AssetDatabase.GUIDToAssetPath(m_PortraitGuid));
                }

                return m_EditorPortrait;
            }
            set
            {
                m_EditorPortrait = value;
                if (value != null)
                {
                    m_PortraitGuid = value.GetGuid().ToString();
                }
            }
        }
#endif

        [Space]
        [SerializeField] private bool    m_Right;
        [SerializeField] private Vector2 m_Offset   = new Vector2(100, 0);
        [SerializeField] private float   m_Duration = .5f;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // m_PortraitGuid     = Portrait.GetGuid().ToString();
            m_PortraitFullPath = AssetDatabase.GUIDToAssetPath(m_PortraitGuid);
            m_PortraitFullPath = m_PortraitFullPath.Replace(EditorPrefix, string.Empty);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (m_PortraitGuid.IsNullOrEmpty())
            {

            }
            else
            {
                m_PortraitFullPath = AssetDatabase.GUIDToAssetPath(m_PortraitGuid);
                m_PortraitFullPath = m_PortraitFullPath.Replace(EditorPrefix, string.Empty);
            }
#endif
        }

        async UniTask IDialogueAttribute.ExecuteAsync(IDialogueData dialogue, IAssetProvider assetProvider,
            IDialogueViewProvider                                           viewProvider)
        {
            var portraitAsset = await assetProvider.LoadAsync<DialogueSpeakerPortrait>(m_PortraitFullPath);
            var portrait      = await assetProvider.LoadAsync<Sprite>(portraitAsset.Object.Portrait);

            IDialogueViewPortrait target;
            if (m_Right)
                target = viewProvider.View.RightPortrait;
            else
                target = viewProvider.View.LeftPortrait;

            if (target.WasIn)
                await target.CrossFadeAndWait(portrait.Object, portraitAsset.Object, m_Duration);
            else
            {
                target.Setup(portrait.Object, portraitAsset.Object);

                Vector2 offset         = m_Offset;
                if (!m_Right) offset.x *= -1f;
                await target.FadeInAndWait(offset, m_Duration);
            }
        }

        public override string ToString()
        {
            string s = m_Right ? "Right" : "Left";
            string n = string.Empty;
#if UNITY_EDITOR
            if (Portrait == null)
                n = string.Empty;
            else
                n = Portrait.name;
#endif

            return $"In {n} {s}: {m_Duration}s";
        }
    }
}