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
// File created : 2024, 05, 25 21:05

#endregion

using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vvr.Model;
using Vvr.Model.Dialogue;

namespace Vvr.ScenarioCreator
{
    class PortraitSpeakerTest : MonoBehaviour
    {
        [Delayed, OnValueChanged(nameof(Setup))]
        [SerializeField] private DialogueSpeakerPortrait m_Speaker;

        private RectTransform PositionTr => (RectTransform) transform.GetChild(0);

        [ShowInInspector]
        public Vector3 Position
        {
            get => PositionTr.anchoredPosition;
            set
            {
                PositionTr.anchoredPosition = value;
                EditorUtility.SetDirty(PositionTr);
            }
        }

        [ShowInInspector]
        public Vector3 Rotation
        {
            get => PositionTr.eulerAngles;
            set
            {
                PositionTr.eulerAngles = value;
                EditorUtility.SetDirty(PositionTr);
            }
        }

        [ShowInInspector]
        public float Scale
        {
            get => PositionTr.localScale.x;
            set
            {
                PositionTr.localScale = Vector3.one * value;
                EditorUtility.SetDirty(PositionTr);
            }
        }

        [Button]
        public void Setup()
        {
            if (m_Speaker == null) return;

            RectTransform tr = PositionTr;
            tr.anchoredPosition = m_Speaker.PositionOffset;
            tr.localScale       = m_Speaker.Scale;
            tr.localRotation    = Quaternion.Euler(m_Speaker.Rotation);
            EditorUtility.SetDirty(tr);

            Image img = tr.GetChild(0).GetComponent<Image>();
            img.sprite = m_Speaker.Portrait.editorAsset as Sprite;
            EditorUtility.SetDirty(img);
        }

        [Button]
        public void Apply()
        {
            RectTransform tr = (RectTransform)transform.GetChild(0);
            m_Speaker.PositionOffset = tr.anchoredPosition;
            m_Speaker.Rotation       = tr.localRotation.eulerAngles;
            m_Speaker.Scale          = tr.localScale;

            EditorUtility.SetDirty(m_Speaker);
        }
    }
}