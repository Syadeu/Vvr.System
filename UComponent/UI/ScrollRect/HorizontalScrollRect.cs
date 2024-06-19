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
// File created : 2024, 06, 12 20:06
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Vvr.UI;

namespace Vvr.UComponent.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(ScrollRect))]
    [PublicAPI, HideMonoScript]
    public class HorizontalScrollRect : MonoBehaviour,
        IScrollRect, IRectLaneContainer,
        IRectTransformPool
    {
        [SerializeField] private RectTransform  m_Prefab;

        [Space]
        [SerializeField] private bool m_ClearOnAwake = true;

        [SerializeField] private int m_InitialCount = 8;
        [SerializeField] private int m_MaxPoolCount = 16;

        private ScrollRect m_ScrollRect;
        private RectProxyLayoutGroup[] m_Lanes;
        private Transform  m_ReservedFolder;

        public ScrollRect ScrollRect
        {
            get
            {
                if (m_ScrollRect is null) m_ScrollRect = GetComponent<ScrollRect>();

                return m_ScrollRect;
            }
        }

        public RectProxyLayoutGroup this[int i] => m_Lanes[i];
        public int             Count => m_Lanes.Sum(x => x.Count);

        int IRectLaneContainer.Count => m_Lanes.Length;

        public Rect      ViewportRect       => ScrollRect.viewport.GetWorldRect();
        public Matrix4x4 ViewportMatrix     => ScrollRect.viewport.worldToLocalMatrix;
        public Vector2   NormalizedPosition => ScrollRect.normalizedPosition;

        private Transform ReservedFolder
        {
            get
            {
                if (m_ReservedFolder is null)
                {
                    GameObject obj = new GameObject("Reserved");
                    m_ReservedFolder = obj.transform;
                    m_ReservedFolder.SetParent(transform, false);

                    obj.SetActive(false);
                }
                return m_ReservedFolder;
            }
        }

        private void Awake()
        {
            // Vector2 itemSizeDelta;
            // if (m_Prefab.TryGetComponent(out ILayoutElement layoutElement))
            // {
            //     itemSizeDelta = new Vector2(
            //         layoutElement.preferredWidth,
            //         layoutElement.flexibleHeight
            //     );
            // }
            // else
            // {
            //     itemSizeDelta = m_Prefab.rect.size;
            // }

            var            content = ScrollRect.content;
            List<RectProxyLayoutGroup> lanes   = new();
            for (int i = 0; i < content.childCount; i++)
            {
                var e = content.GetChild(i);
                if (!e.TryGetComponent(out RectProxyLayoutGroup lane)) continue;

                // lane.ItemSizeDelta = itemSizeDelta;

                lane.Viewport = ScrollRect.viewport;
                lanes.Add(lane);
            }

            if (lanes.Count == 0)
                lanes.Add(content.GetOrAddComponent<RectProxyLayoutGroup>());
            m_Lanes = lanes.ToArray();
            if (m_ClearOnAwake)
            {
                foreach (var t in m_Lanes)
                {
                    t.transform.ClearChildren();
                }
            }

            for (int i = 0; i < m_InitialCount; i++)
            {
                Return(Rent());
            }

            ScrollRect.onValueChanged.AddListener(OnScrollEvent);
        }

        public RectTransform Rent()
        {
            int count = ReservedFolder.childCount;
            if (count <= 0)
                return Instantiate(m_Prefab);

            return ReservedFolder.GetChild(count - 1) as RectTransform;
        }
        public void Return(RectTransform t)
        {
            t.SetParent(ReservedFolder, false);
        }

        private void OnScrollEvent(Vector2 normalizedPosition)
        {
            for (int i = 0; i < m_Lanes.Length; i++)
            {
                var e = m_Lanes[i];
                e.UpdateProxy();
            }
        }

        IEnumerable<RectProxyLayoutGroup> IRectLaneContainer.GetEnumerable() => m_Lanes;
        public IEnumerator<IRectItem> GetEnumerator()
        {
            foreach (var lane in m_Lanes)
            {
                foreach (var item in lane)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}