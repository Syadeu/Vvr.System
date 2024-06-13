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
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Vvr.UI;

namespace Vvr.UComponent.UI
{
    // TODO: currently, horizontal only
    [RequireComponent(typeof(RectTransform))]
    [HideMonoScript]
    public class RectLane : LayoutGroup
    {
        [SerializeField] private float      m_Spacing;

        private IRectTransformPool m_Pool;
        private IScrollRect        m_ScrollRect;

        private readonly LinkedList<IScrollRectItem>                m_Items = new();
        private readonly Dictionary<IScrollRectItem, RectTransform> m_Proxy = new();

        private IRectTransformPool Pool => m_Pool ??= GetComponentInParent<IRectTransformPool>();
        private IScrollRect ScrollRect => m_ScrollRect ??= GetComponentInParent<IScrollRect>();

        public int Count => m_Items.Count;

        [PublicAPI]
        public void Add(IScrollRectItem item)
        {
            m_Items.AddLast(item);
            UpdateProxy();
        }
        [PublicAPI]
        public bool Remove(IScrollRectItem item)
        {
            if (!m_Items.Remove(item)) return false;
            UpdateProxy();
            return true;
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            float calculatedWidth = 0;
            int   count           = 0;

            if (!Application.isPlaying)
            {
                foreach (var child in rectChildren)
                {
                    if (count++ > 0) calculatedWidth += m_Spacing;

                    calculatedWidth += LayoutUtility.GetPreferredSize(child, 0);
                }
            }
            else
            {
                foreach (var item in m_Items)
                {
                    if (count++ > 0) calculatedWidth += m_Spacing;

                    calculatedWidth += item.PreferredSizeDelta.x;
                }
            }
            SetLayoutInputForAxis(calculatedWidth, calculatedWidth, 0, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            float calculatedHeight = 0;
            foreach (var item in m_Items)
            {
                calculatedHeight = Mathf.Max(item.PreferredSizeDelta.y, calculatedHeight);
            }

            foreach (var child in rectChildren)
            {
                calculatedHeight = Mathf.Max(calculatedHeight,
                    LayoutUtility.GetPreferredSize(child, 1));
            }

            SetLayoutInputForAxis(calculatedHeight, calculatedHeight, 0, 1);
        }

        public override void SetLayoutHorizontal()
        {
            if (!Application.isPlaying)
            {
                float startOffset = GetStartOffset(0, GetTotalPreferredSize(0) - m_Padding.horizontal);

                foreach (var child in rectChildren)
                {
                    float preferred = LayoutUtility.GetPreferredSize(child, 0);

                    SetChildAlongAxisWithScale(child, 0, startOffset, preferred, 1);
                    startOffset += preferred + m_Spacing;
                }

                return;
            }

            int i = 0;
            foreach (var pos in GetVisiblePositionWithAxis(0))
            {
                if (rectChildren.Count <= i) break;

                var child = rectChildren[i++];

                float preferred = LayoutUtility.GetPreferredSize(child, 0);

                SetChildAlongAxisWithScale(child, 0, pos.pos, preferred, 1);
            }
        }
        public override void SetLayoutVertical()
        {
            float startOffset = GetStartOffset(1, GetTotalPreferredSize(1) - m_Padding.vertical);

            foreach (var child in rectChildren)
            {
                float preferred = LayoutUtility.GetPreferredSize(child, 1);

                SetChildAlongAxisWithScale(child, 1, startOffset, preferred, 1);
            }
        }

        private IEnumerable<(bool visible, float pos)> GetVisiblePositionWithAxis(int axis, bool visibleOnly = true)
        {
            float pos = GetStartOffset(axis, GetTotalPreferredSize(0) - m_Padding.horizontal);

            Rect rect = rectTransform.GetWorldRect();

            Matrix4x4 viewMatrix    = ScrollRect.ViewportMatrix;
            Vector3   worldStartPos = rectTransform.GetWorldCenterPosition() - new Vector3(rect.size.x * .5f, 0);
            Vector3
                localStartPos = viewMatrix.MultiplyPoint(worldStartPos),
                localEndPos   = ScrollRect.ViewportRect.size
                ;

            for (var currentNode = m_Items.First;
                 currentNode != null;
                 currentNode = currentNode.Next
                )
            {
                Vector2 sizeDelta = currentNode.Value.PreferredSizeDelta;

                float start = axis == 0 ? localStartPos.x : localStartPos.y;
                float end   = axis == 0 ? localEndPos.x : localEndPos.y;
                float v     = (axis == 0 ? sizeDelta.x : sizeDelta.y) + m_Spacing;

                bool isVisible = 0 < start + v && start + v < end + v * 2;

                if (visibleOnly)
                {
                    if (isVisible)
                        yield return (true, pos);
                }
                else
                    yield return (isVisible, pos);

                pos += v;
                if (axis == 0)
                    localStartPos.x += v;
                else
                    localStartPos.y += v;
            }
        }

        public void UpdateProxy()
        {
            if (m_Items.Count <= 0) return;

            int count = 0;

            var node = m_Items.First;
            foreach (var xPos in GetVisiblePositionWithAxis(0, false))
            {
                RectTransform proxy;
                if (xPos.visible)
                {
                    if (m_Proxy.TryGetValue(node.Value, out proxy))
                    {
                        proxy.SetSiblingIndex(count);
                    }
                    else
                    {
                        proxy = Pool.Rent();
                        proxy.SetParent(rectTransform, false);

                        m_Proxy[node.Value] = proxy;

                        node.Value.Bind(proxy);
                    }
                }
                else
                {
                    if (m_Proxy.Remove(node.Value, out proxy))
                    {
                        node.Value.Unbind(proxy);
                        Pool.Return(proxy);
                    }
                }

                node = node.Next;
                count++;
                if (node is null) break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (ScrollRect is null) return;

            RectTransform tr    = (RectTransform)transform;
            Rect          rect  = tr.GetWorldRect();
            Vector3       scale = tr.lossyScale;

            float xSpacing = m_Spacing * scale.x;

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
                currentRect.x    += currentRect.size.x + xSpacing;
            }
        }
#endif

        private Vector3 GetWorldStartPosition()
        {
            Rect rect = rectTransform.GetWorldRect();

            return rectTransform.GetWorldCenterPosition() - new Vector3(rect.size.x * .5f, 0);
        }
        private Rect GetStartRect()
        {
            if (m_Items.Count <= 0) return default;

            RectTransform tr    = (RectTransform)transform;
            Rect          rect  = tr.GetWorldRect();

            LinkedListNode<IScrollRectItem> currentNode = m_Items.First;

            Vector2 sizeDelta = currentNode.Value.PreferredSizeDelta;
            sizeDelta = tr.TransformVector(sizeDelta);

            Rect currentRect = new Rect(
                tr.GetWorldCenterPosition() - new Vector3(rect.size.x * .5f, 0),
                sizeDelta);

            currentRect.x += padding.left;
            currentRect.x += currentRect.size.x * .5f;

            return currentRect;
        }
    }

    [PublicAPI]
    public interface IRectLaneContainer
    {
        RectLane this[int i] { get; }
        int Count { get; }

        IEnumerable<RectLane> GetEnumerable();
    }
}