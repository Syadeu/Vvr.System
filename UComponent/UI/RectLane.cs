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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vvr.UI;

namespace Vvr.UComponent.UI
{
    // TODO: currently, horizontal only
    [RequireComponent(typeof(RectTransform))]
    public class RectLane : LayoutGroup
    {
        [SerializeField] private float m_Spacing;

        private IRectTransformPool m_Pool;
        private IScrollRect        m_ScrollRect;

        private HorizontalOrVerticalLayoutGroup m_LayoutGroup;

        private readonly LinkedList<IScrollRectItem> m_Items = new();

        private float m_CalculatedWidth;
        private float m_CalculatedHeight;

        private IRectTransformPool Pool => m_Pool ??= GetComponentInParent<IRectTransformPool>();

        private IScrollRect ScrollRect => m_ScrollRect ??= GetComponentInParent<IScrollRect>();

        private HorizontalOrVerticalLayoutGroup LayoutGroup
        {
            get
            {
                if (m_LayoutGroup is null) m_LayoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
                return m_LayoutGroup;
            }
        }

        public override float preferredWidth
        {
            get
            {
                if (!Application.isPlaying) return base.preferredWidth;

                return m_CalculatedWidth;
            }
        }

        public void Add(IScrollRectItem item)
        {
            m_Items.AddLast(item);
            UpdateProxy(ScrollRect.ViewportRect, ScrollRect.NormalizedPosition);
            "add".ToLog();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            if (!Application.isPlaying)
            {
                base.CalculateLayoutInputHorizontal();
                return;
            }

            int count = 0;
            m_CalculatedWidth = 0;
            foreach (var item in m_Items)
            {
                if (count++ > 0) m_CalculatedWidth += m_Spacing;

                m_CalculatedWidth += item.PreferredSizeDelta.x;
            }

            SetLayoutInputForAxis(m_CalculatedWidth, m_CalculatedWidth, 0, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            m_CalculatedHeight = 0;
            foreach (var item in m_Items)
            {
                m_CalculatedHeight = Mathf.Max(item.PreferredSizeDelta.y, m_CalculatedHeight);
            }
        }

        public override void SetLayoutHorizontal()
        {
        }
        public override void SetLayoutVertical()
        {

        }

        public void UpdateProxy(Rect viewportRect, Vector2 normalizedPosition)
        {
            if (m_Items.Count <= 0) return;

            RectTransform tr            = (RectTransform)transform;
            int           existingCount = tr.childCount;
            Rect          rect          = tr.GetWorldRect();

            // Vector2 visibleSizeDelta   = viewportRect.size;
            // Vector2 offset             = rect.size - visibleSizeDelta;
            // if (offset.x < 0) offset.x = 0;
            // if (offset.y < 0) offset.y = 0;

            int  count       = 0;
            Rect currentRect = GetStartRect();
            // horizontal
            for (var node = m_Items.First;
                 node != null;

                 node = node.Next
                 )
            {
                // if (currentRect.x < visibleStart.x) continue;

                if (viewportRect.Overlaps(currentRect))
                {
                    RectTransform proxy;
                    if (existingCount <= count)
                    {
                        proxy = Pool.Rent();
                        proxy.SetParent(tr, false);
                    }
                    else
                    {
                        proxy = (RectTransform)tr.GetChild(count);
                    }

                    node.Value.Bind(proxy);
                    count++;
                }

                currentRect.x    += node.Value.PreferredSizeDelta.x + (LayoutGroup?.spacing ?? 0);
                currentRect.size =  node.Value.PreferredSizeDelta;
            }

            for (int i = existingCount - 1; i >= count; i--)
            {
                Pool.Return((RectTransform)tr.GetChild(i));
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (ScrollRect is null) return;

            RectTransform tr   = (RectTransform)transform;
            Rect          rect = tr.GetWorldRect();

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(tr.GetWorldCenterPosition(), rect.size);

            if (!Application.isPlaying)
            {
                var xx = transform.childCount;
                for (int x = 0; x < xx; x++)
                {
                    var e = (RectTransform)transform.GetChild(x);

                    Rect itemRect = e.GetWorldRect();

                    bool overlap = itemRect.Overlaps(ScrollRect.ViewportRect, false);
                    if (!overlap)
                    {
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;
                    }

                    Gizmos.DrawWireCube(e.position, itemRect.size);
                }
                return;
            }

            if (m_Items.Count <= 0) return;

            Rect currentRect = GetStartRect();
            for (var currentNode = m_Items.First;
                 currentNode != null;

                 currentNode = currentNode.Next
                )
            {
                bool overlap = currentRect.Overlaps(ScrollRect.ViewportRect, false);
                if (!overlap)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }

                Gizmos.DrawWireCube(currentRect.position, currentRect.size);
                currentRect.x    += currentNode.Value.PreferredSizeDelta.x + (LayoutGroup?.spacing ?? 0);
                currentRect.size =  currentNode.Value.PreferredSizeDelta;
            }
        }
#endif

        private Rect GetStartRect()
        {
            RectTransform tr   = (RectTransform)transform;
            Rect          rect = tr.GetWorldRect();

            LinkedListNode<IScrollRectItem> currentNode = m_Items.First;
            Rect currentRect = new Rect(
                tr.GetWorldCenterPosition() - new Vector3(rect.size.x * .5f, 0),
                currentNode.Value.PreferredSizeDelta);
            currentRect.x += currentRect.size.x * .5f;

            return currentRect;
        }
    }


    public interface IRectTransformPool
    {
        RectTransform Rent();

        void Return(RectTransform t);
    }

    public interface IScrollRect
    {
        Rect    ViewportRect       { get; }
        Vector2 NormalizedPosition { get; }
    }
    public interface IScrollRectItem
    {
        Vector2 PreferredSizeDelta { get; }

        void Bind(RectTransform t);
    }
}