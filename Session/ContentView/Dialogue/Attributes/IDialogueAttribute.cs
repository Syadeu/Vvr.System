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
// File created : 2024, 05, 26 17:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    public interface IDialogueAttribute
    {
        DialogueAttributeType AttributeType { get; }

        UniTask ExecuteAsync(IDialogueData dialogue, IAssetProvider assetProvider, IDialogueViewProvider viewProvider);
    }

    [Serializable]
    sealed class RawDialogueAttribute : ISerializationCallbackReceiver
    {
        [Delayed, OnValueChanged(nameof(Resolve))]
        [SerializeField] private DialogueAttributeType m_Type;
        [HideInInspector]
        [SerializeField] private string                m_Json;

        private IDialogueAttribute m_Attribute;

        [ShowIf("@m_Attribute != null"), FoldoutGroup("@"+nameof(DisplayName), GroupID = "Attribute")]
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

        private string DisplayName => m_Attribute == null ? "Attribute" : m_Attribute.ToString();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (Value == null)
            {
                // m_Type = 0;
                // m_Json = null;
                return;
            }

            m_Type = Value.AttributeType;
            m_Json = JsonUtility.ToJson(Value);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Resolve();
        }

        private void Resolve()
        {
            Type type;
            if (m_Type == DialogueAttributeType.Speaker)
                type = VvrTypeHelper.TypeOf<DialogueSpeakerAttribute>.Type;
            else if (m_Type == DialogueAttributeType.Wait)
                type = VvrTypeHelper.TypeOf<DialougeWaitAttribute>.Type;
            else if (m_Type == DialogueAttributeType.PortraitIn)
                type = VvrTypeHelper.TypeOf<DialoguePortraitInAttribute>.Type;
            else if (m_Type == DialogueAttributeType.PortraitOut)
                type = VvrTypeHelper.TypeOf<DialoguePortraitOutAttribute>.Type;
            else
            {
                m_Attribute = null;
                return;
            }

            m_Attribute =
                m_Json.IsNullOrEmpty()
                    ? Activator.CreateInstance(type) as IDialogueAttribute
                    : JsonUtility.FromJson(m_Json, type) as IDialogueAttribute;
        }
    }

    public enum DialogueAttributeType
    {
        None = 0,

        Speaker,
        Wait,

        PortraitIn,
        PortraitOut,
    }
}