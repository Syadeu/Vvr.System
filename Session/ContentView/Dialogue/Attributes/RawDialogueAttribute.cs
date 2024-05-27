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
// File created : 2024, 05, 26 22:05

#endregion

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [Serializable]
    sealed class RawDialogueAttribute : ISerializationCallbackReceiver
    {
        [InfoBox(
            "Type resolve has been failed. You should specify target type manually.",
            InfoMessageType.Error,
            visibleIfMemberName: nameof(m_TypeResolveFailed))]
        [OnValueChanged(nameof(Resolve))]
        [ValueDropdown(
            nameof(GetTypeNameList), IsUniqueList = true, OnlyChangeValueOnConfirm = true)]
        [SerializeField] private string m_TypeName;

        [HideInInspector]
        [SerializeField] private bool m_TypeResolveFailed;

        [HideInInspector] [SerializeField] private string m_Json;

        private IDialogueAttribute m_Attribute;

        [HideIf("@" + nameof(m_TypeResolveFailed))]
        [FoldoutGroup("@"+nameof(DisplayName), GroupID = "Attribute")]
        [ShowInInspector, InlineProperty, HideLabel]
        [HideReferenceObjectPicker]
        public IDialogueAttribute Value
        {
            get
            {
                if (m_Attribute == null) Resolve();

                return m_Attribute;
            }
            private set => m_Attribute = value;
        }
#if UNITY_EDITOR
        private ValueDropdownList<string> GetTypeNameList() => DialogueAttributeHelper.GetDropdownList();
#endif

        private string DisplayName => m_Attribute == null ? string.Empty : m_Attribute.ToString();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (Value == null || m_TypeResolveFailed)
            {
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying) return;

            m_TypeName = Value.GetType().AssemblyQualifiedName;
            m_Json     = JsonUtility.ToJson(Value);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        [OnInspectorGUI, ShowIf(nameof(m_TypeResolveFailed))]
        private void OnInspectorGUI()
        {
            GUI.enabled = false;
            GUILayout.TextArea(m_TypeName);
            GUI.enabled = true;
        }

        private void Resolve()
        {
            if (m_TypeName.IsNullOrEmpty())
            {
                m_Attribute = null;
                return;
            }

            if (!DialogueAttributeHelper.AttributeTypeMap.TryGetValue(m_TypeName, out Type type))
            {
                m_TypeResolveFailed = true;
                return;
            }

            m_TypeResolveFailed = false;

            m_Attribute =
                m_Json.IsNullOrEmpty()
                    ? Activator.CreateInstance(type) as IDialogueAttribute
                    : JsonUtility.FromJson(m_Json, type) as IDialogueAttribute;
        }
    }
}