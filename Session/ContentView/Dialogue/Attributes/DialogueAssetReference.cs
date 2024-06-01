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
    /// <summary>
    /// Represents a reference to a dialogue asset.
    /// </summary>
    /// <typeparam name="TObject">The type of the dialogue asset.</typeparam>
    [PublicAPI, Serializable]
    [HideReferenceObjectPicker, InlineProperty]
    public sealed class DialogueAssetReference<TObject> : IValidate, ISerializationCallbackReceiver
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

        /// <summary>
        /// Represents a reference to an editor asset.
        /// </summary>
        /// <typeparam name="TObject">The type of the editor asset.</typeparam>
        /// <remarks>
        /// <para>
        /// The <see cref="EditorAsset"/> property allows the editor asset to be loaded
        /// at runtime in the Unity Editor.
        /// </para>
        /// <para>
        /// The reference to the editor asset is stored using its GUID in the Unity asset database.
        /// </para>
        /// </remarks>
        [ShowInInspector, InlineProperty, HideLabel]
        public TObject EditorAsset
        {
            get
            {
#if UNITY_EDITOR
                if (m_EditorAsset is null &&
                    !m_AssetGuid.IsNullOrEmpty())
                {
                    m_EditorAsset = AssetDatabase.LoadAssetAtPath<TObject>(
                        AssetDatabase.GUIDToAssetPath(m_AssetGuid));
                }

                return m_EditorAsset;
#else
                return null;
#endif
            }
#if UNITY_EDITOR
            private set
            {
                m_EditorAsset = value;
                if (m_EditorAsset is not null)
                {
                    m_AssetGuid
                        = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(m_EditorAsset)).ToString();
                }
                else m_AssetGuid = string.Empty;
            }
#endif
        }

        /// <summary>
        /// Gets the full path of the asset.
        /// </summary>
        /// <value>
        /// The full path of the asset.
        /// </value>
        public string FullPath => m_AssetFullPath;

        public bool IsValid()
        {
            if (m_AssetFullPath.IsNullOrEmpty()) return false;
#if UNITY_EDITOR
            if (m_AssetGuid.IsNullOrEmpty() || EditorAsset == null) return false;
#endif
            return true;
        }

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