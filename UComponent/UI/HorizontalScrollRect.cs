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

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vvr.UI;

namespace Vvr.UComponent.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(ScrollRect))]
    public class HorizontalScrollRect : MonoBehaviour,
        IScrollRect,
        IRectTransformPool
    {
        [SerializeField] private ScrollRectItem testItem;
        [SerializeField] private RectTransform  testPrefab;

        [SerializeField] private bool m_ClearOnAwake = true;

        private ScrollRect m_ScrollRect;
        private RectLane[] m_Lanes;
        private Transform  m_ReservedFolder;

        public ScrollRect ScrollRect
        {
            get
            {
                if (m_ScrollRect is null) m_ScrollRect = GetComponent<ScrollRect>();

                return m_ScrollRect;
            }
        }

        public RectLane this[int i] => m_Lanes[i];
        public Rect    ViewportRect       => ScrollRect.viewport.GetWorldRect();
        public Vector2 NormalizedPosition => ScrollRect.normalizedPosition;

        private Transform ReservedFolder
        {
            get
            {
                if (m_ReservedFolder is null)
                {
                    GameObject obj = new GameObject("Reserved");
                    m_ReservedFolder = obj.transform;
                    m_ReservedFolder.SetParent(transform);
                }
                return m_ReservedFolder;
            }
        }

        private void Awake()
        {
            var            content = ScrollRect.content;
            List<RectLane> lanes   = new();
            for (int i = 0; i < content.childCount; i++)
            {
                var e = content.GetChild(i);
                if (!e.TryGetComponent(out RectLane lane)) continue;

                lanes.Add(lane);
            }

            if (lanes.Count == 0)
                lanes.Add(content.GetOrAddComponent<RectLane>());
            m_Lanes = lanes.ToArray();
            if (m_ClearOnAwake)
            {
                foreach (var t in m_Lanes)
                {
                    t.transform.ClearChildren();
                }
            }

            ScrollRect.onValueChanged.AddListener(OnScrollEvent);
        }

        public RectTransform Rent()
        {
            return Instantiate(testPrefab);
        }
        public void Return(RectTransform t)
        {
            Destroy(t.gameObject);
        }

        private void OnScrollEvent(Vector2 normalizedPosition)
        {
            RectTransform viewport     = ScrollRect.viewport;
            Rect          viewportRect = viewport.GetWorldRect();

            for (int i = 0; i < m_Lanes.Length; i++)
            {
                var e = m_Lanes[i];
                e.UpdateProxy(viewportRect, normalizedPosition);
            }
        }

        public void Add(IScrollRectItem item)
        {

        }

        [Button, ResponsiveButtonGroup]
        public void TestAdd()
        {
            this[0].Add((ScrollRectItem)testItem.Clone());
            this[1].Add((ScrollRectItem)testItem.Clone());
        }
[Button, ResponsiveButtonGroup]
        public void TestRemove()
        {

        }
//
// #if UNITY_EDITOR
//         private void OnDrawGizmosSelected()
//         {
//             var content = ScrollRect.content;
//             int yy      = content.childCount;
//
//             RectTransform viewport     = ScrollRect.viewport;
//             Rect          viewportRect = viewport.GetWorldRect();
//
//             Gizmos.color = Color.red;
//             Gizmos.DrawWireCube(viewport.GetWorldCenterPosition(), viewportRect.size);
//
//             for (int i = 0; i < yy; i++)
//             {
//                 var row = (RectTransform)content.GetChild(i);
//
//                 int xx = row.childCount;
//                 for (int x = 0; x < xx; x++)
//                 {
//                     var e = (RectTransform)row.GetChild(x);
//
//                     Rect rect = e.GetWorldRect();
//
//                     bool overlap = rect.Overlaps(viewportRect, false);
//                     if (!overlap)
//                     {
//                         Gizmos.color = Color.red;
//                     }
//                     else
//                     {
//                         Gizmos.color = Color.yellow;
//                     }
//
//                     Gizmos.DrawWireCube(e.position, rect.size);
//                 }
//             }
//         }
// #endif


    }
}