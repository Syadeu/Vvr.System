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
// File created : 2024, 06, 14 14:06

#endregion

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Vvr.UComponent.UI
{
    [DisallowMultipleComponent]
    [HideMonoScript]
    public class HorizontalTabLayoutGroup : MonoBehaviour
    {
        [Serializable]
        struct TabLink
        {
            [ReadOnly]
            public RectTransform tab;
            [ReadOnly]
            public RectTransform content;

            [SerializeField] public UnityEvent<RectTransform> onDeselect;
            [SerializeField] public UnityEvent<RectTransform> onSelect;
        }

        class TabMenuItem : MonoBehaviour, IPointerClickHandler
        {
            private HorizontalTabLayoutGroup m_LayoutGroup;

            public void Setup(HorizontalTabLayoutGroup g)
            {
                m_LayoutGroup = g;
            }

            void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
            {
                m_LayoutGroup.SelectedIndex = transform.GetSiblingIndex();
            }
        }

        [ChildGameObjectsOnly(IncludeSelf = false)] [SerializeField, Required]
        private RectTransform m_TabGroup;
        [ChildGameObjectsOnly(IncludeSelf = false)] [SerializeField, Required]
        private RectTransform m_ContentGroup;

        [Space]
        [MinValue(0), MaxValue("@m_Links.Length - 1")]
        [OnValueChanged(nameof(UpdateContent))]
        [SerializeField] private int m_SelectedIndex;

        [Space]
        [ListDrawerSettings(
            ShowIndexLabels = true,
            Expanded = true,
            HideAddButton = true, HideRemoveButton = true, DraggableItems = false, IsReadOnly = true)]
        [SerializeField] private TabLink[] m_Links;

        public int TabCount => m_Links.Length;
        public int SelectedIndex
        {
            get => m_SelectedIndex;
            set
            {
                if (m_SelectedIndex == value) return;

                m_Links[m_SelectedIndex].onDeselect.Invoke(m_Links[m_SelectedIndex].content);
                m_SelectedIndex = Mathf.Clamp(value, 0, m_Links.Length - 1);
                UpdateContent();

                m_Links[m_SelectedIndex].onSelect.Invoke(m_Links[m_SelectedIndex].content);
            }
        }

        private void Awake()
        {
            for (int i = 0; i < m_Links.Length; i++)
            {
                TabLink e = m_Links[i];
                e.tab.AddComponent<TabMenuItem>().Setup(this);
            }
        }

#if UNITY_EDITOR
        void SetProperty<T>(ref T p, T v)
        {
            if (p.Equals(v)) return;

            p = v;
            EditorUtility.SetDirty(this);
        }

        [OnInspectorInit]
        void OnInspectorInit()
        {
            if (m_TabGroup == null || m_ContentGroup == null) return;

            if (m_Links.Length != m_TabGroup.childCount)
            {
                m_Links = new TabLink[m_TabGroup.childCount];
                EditorUtility.SetDirty(this);
            }

            int count = m_ContentGroup.childCount;
            for (int i = 0; i < m_Links.Length; i++)
            {
                SetProperty(ref m_Links[i].tab, (RectTransform)m_TabGroup.GetChild(i));
                if (count < i)
                {
                    SetProperty(ref m_Links[i].content, (RectTransform)m_ContentGroup.GetChild(i));
                }
            }
        }
#endif

        private void UpdateContent()
        {
            if (m_Links.Length <= m_SelectedIndex)
            {
                return;
            }

            for (int i = 0; i < m_SelectedIndex; i++)
            {
                m_Links[i].content.gameObject.SetActive(false);
            }
            m_Links[m_SelectedIndex].content.gameObject.SetActive(true);
            for (int i = m_SelectedIndex + 1; i < m_Links.Length; i++)
            {
                m_Links[i].content.gameObject.SetActive(false);
            }
        }
    }
}