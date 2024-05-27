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

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [PublicAPI, Serializable]
    [HideReferenceObjectPicker, InlineProperty]
    public sealed class DialogueAssetReference<TObject> : ISerializationCallbackReceiver
        where TObject : UnityEngine.Object
    {
#if UNITY_EDITOR
        const string EditorPrefix = "Assets/AddressableResources/";

        private TObject m_EditorAsset;
#endif

        [HideInInspector]
        [SerializeField] private string m_AssetGuid;

        [HideInInspector]
        [SerializeField] private string m_AssetFullPath;

#if UNITY_EDITOR
        [ShowInInspector, InlineProperty, HideLabel]
        public TObject EditorAsset
        {
            get
            {
                if (m_EditorAsset == null &&
                    !m_AssetGuid.IsNullOrEmpty())
                {
                    m_EditorAsset = AssetDatabase.LoadAssetAtPath<TObject>(
                        AssetDatabase.GUIDToAssetPath(m_AssetGuid));
                }

                return m_EditorAsset;
            }
            set
            {
                m_EditorAsset = value;
                if (m_EditorAsset != null)
                {
                    m_AssetGuid
                        = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(m_EditorAsset)).ToString();
                }
            }
        }
#endif
        public string FullPath => m_AssetFullPath;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (!m_AssetGuid.IsNullOrEmpty())
            {
                m_AssetFullPath = AssetDatabase.GUIDToAssetPath(m_AssetGuid);
                m_AssetFullPath = m_AssetFullPath.Replace(EditorPrefix, string.Empty);
            }
#endif
        }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (!m_AssetGuid.IsNullOrEmpty())
            {
                m_AssetFullPath = AssetDatabase.GUIDToAssetPath(m_AssetGuid);
                m_AssetFullPath = m_AssetFullPath.Replace(EditorPrefix, string.Empty);
            }
#endif
        }

        public override string ToString() => m_AssetFullPath;
    }
}