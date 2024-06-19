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
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Vvr.UI;

namespace Vvr.UComponent.UI
{
    // TODO: currently, horizontal only
    [RequireComponent(typeof(RectTransform))]
    [HideMonoScript]
    public class RectLane : LayoutGroup, IEnumerable<IRectItem>
    {
        private enum Direction : short
        {
            Horizontal,
            Vertical
        }

        [SerializeField] private Direction m_Direction = Direction.Horizontal;
        [SerializeField] private float     m_Spacing;

        [Space] [SerializeField] private RectTransform m_Viewport;

        private IRectTransformPool m_Pool;

        private Vector2? m_ItemSizeDelta;

        private readonly LinkedList<IRectItem>                m_Items = new();
        private readonly Dictionary<IRectItem, RectTransform> m_Proxy = new();

        private IRectTransformPool Pool => m_Pool ??= GetComponentInParent<IRectTransformPool>();

        public int     Count         => m_Items.Count;
        [ShowInInspector, ReadOnly]
        public Vector2 ItemSizeDelta { get => m_ItemSizeDelta ?? -Vector2.one; set => m_ItemSizeDelta = value; }
        public RectTransform Viewport
        {
            get => m_Viewport;
            set => m_Viewport = value;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_Viewport == null) m_Viewport = rectTransform;
        }

        [PublicAPI]
        public void Insert(int index, [NotNull] IRectItem item)
        {
            Assert.IsNotNull(item);

            int i = 0;
            for (var node = m_Items.First;
                 node != null;
                 node = node.Next, i++)
            {
                if (index > i) continue;

                m_Items.AddBefore(node, item);
                UpdateProxy();
                return;
            }

            Add(item);
        }
        [PublicAPI]
        public void Add([NotNull] IRectItem item)
        {
            Assert.IsNotNull(item);

            m_Items.AddLast(item);
            UpdateProxy();
        }
        [PublicAPI]
        public bool Remove([NotNull] IRectItem item)
        {
            Assert.IsNotNull(item);

            if (!m_Items.Remove(item)) return false;
            UpdateProxy();
            return true;
        }

        public void Clear()
        {
            m_Items.Clear();
            UpdateProxy();
        }

        private float CalculateSizeForAxis(int axis)
        {
            float calculated = 0;
            if (((int)m_Direction ^ axis) == 0)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    int count = 0;
                    foreach (var child in rectChildren)
                    {
                        if (count++ > 0) calculated += m_Spacing;

                        calculated += LayoutUtility.GetPreferredSize(child, axis);
                    }

                    return calculated;
                }
#endif
                if (ItemSizeDelta[axis]    < 0 &&
                    rectChildren.Count > 0)
                {
                    m_ItemSizeDelta = new Vector2(
                        LayoutUtility.GetPreferredSize(rectChildren[0], axis),
                        ItemSizeDelta[axis ^ 1]);
                }

                calculated += m_Spacing       * (m_Items.Count - 1);
                calculated += ItemSizeDelta[axis] * m_Items.Count;
            }
            else
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    foreach (var child in rectChildren)
                    {
                        calculated = Mathf.Max(calculated,
                            LayoutUtility.GetPreferredSize(child, axis));
                    }

                    return calculated;
                }
#endif
                if (ItemSizeDelta[axis]    < 0 &&
                    rectChildren.Count > 0)
                {
                    float yy = -1;
                    for (int i = 0; i < rectChildren.Count; i++)
                    {
                        yy = Mathf.Max(yy, LayoutUtility.GetPreferredSize(rectChildren[i], axis));
                    }

                    m_ItemSizeDelta = new Vector2(
                        ItemSizeDelta[axis ^ 1],
                        yy);
                }

                calculated = ItemSizeDelta[axis];
            }

            return calculated;
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            float calculatedWidth = CalculateSizeForAxis(0);

            SetLayoutInputForAxis(calculatedWidth, calculatedWidth, 0, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            float calculatedHeight = CalculateSizeForAxis(1);

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

            // m_ItemSizeDelta.x = 0;
            int i = 0;
            foreach (var pos in GetVisiblePositionWithAxis(0))
            {
                if (rectChildren.Count <= i) break;

                var child = rectChildren[i++];

                float preferred = LayoutUtility.GetPreferredSize(child, 0);
                // m_ItemSizeDelta.x = Mathf.Max(m_ItemSizeDelta.x, preferred);

                SetChildAlongAxisWithScale(child, 0, pos.pos, preferred, 1);
            }
        }
        public override void SetLayoutVertical()
        {
            float startOffset = GetStartOffset(1, GetTotalPreferredSize(1) - m_Padding.vertical);

            // m_ItemSizeDelta.y = 0;
            foreach (var child in rectChildren)
            {
                float preferred = LayoutUtility.GetPreferredSize(child, 1);
                // m_ItemSizeDelta.y = Mathf.Max(m_ItemSizeDelta.y, preferred);

                SetChildAlongAxisWithScale(child, 1, startOffset, preferred, 1);
            }
        }

        private IEnumerable<(bool visible, float pos)> GetVisiblePositionWithAxis(int axis, bool visibleOnly = true)
        {
            float pos = GetStartOffset(
                axis,
                GetTotalPreferredSize(axis)
                - (axis == 0 ? m_Padding.horizontal : m_Padding.vertical));
            float nPos = GetStartOffset(
                axis ^ 1,
                GetTotalPreferredSize(axis ^ 1)
                - ((axis ^ 1) == 0 ? m_Padding.horizontal : m_Padding.vertical));
            Vector2 localPos = axis == 0 ? new Vector2(pos, nPos) : new Vector2(nPos, pos);

            Matrix4x4 viewMatrix    = m_Viewport.worldToLocalMatrix;
            Vector3
                localStartPos = viewMatrix.MultiplyPoint(transform.TransformPoint(localPos)) - transform.localPosition,
                localEndPos   = m_Viewport.GetActualSizeDelta()
                ;

            for (var currentNode = m_Items.First;
                 currentNode != null;
                 currentNode = currentNode.Next
                )
            {
                Vector2 sizeDelta = ItemSizeDelta;

                float start = localStartPos[axis];
                float end   = localEndPos[axis];
                float v     = sizeDelta[axis] + m_Spacing;

                bool isVisible = 0 < start + v && start + v < end + v * 2;

                if (visibleOnly)
                {
                    if (isVisible)
                        yield return (true, pos);
                }
                else
                    yield return (isVisible, pos);

                pos += v;
                localStartPos[axis] += v;
            }
        }

        public void UpdateProxy()
        {
            if (m_Items.Count <= 0)
            {
                foreach (var item in m_Proxy)
                {
                    item.Key.Unbind(item.Value);
                    Pool.Return(item.Value);
                }
                m_Proxy.Clear();
                return;
            }

            int count        = 0;
            int visibleCount = 0;

            var node = m_Items.First;
            foreach (var xPos in GetVisiblePositionWithAxis(0, false))
            {
                Assert.IsNotNull(node);
                Assert.IsNotNull(node.Value);

                RectTransform proxy;
                if (xPos.visible)
                {
                    if (!m_Proxy.TryGetValue(node.Value, out proxy))
                    {
                        proxy = Pool.Rent();
                        proxy.SetParent(rectTransform, false);

                        m_Proxy[node.Value] = proxy;

                        node.Value.Bind(proxy);
                    }

                    proxy.SetSiblingIndex(visibleCount);
                    visibleCount++;
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
            RectTransform tr    = (RectTransform)transform;
            Rect          rect  = tr.GetWorldRect();
            Vector3       scale = tr.lossyScale;

            float xSpacing = m_Spacing * scale.x;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(tr.GetWorldCenterPosition(), rect.size);

            // if (!Application.isPlaying)
            // {
            //     var xx = transform.childCount;
            //     for (int x = 0; x < xx; x++)
            //     {
            //         var e = (RectTransform)transform.GetChild(x);
            //
            //         Rect itemRect = e.GetWorldRect();
            //
            //         bool overlap = itemRect.Overlaps(ScrollRect.ViewportRect, false);
            //         if (!overlap)
            //         {
            //             Gizmos.color = Color.red;
            //         }
            //         else
            //         {
            //             Gizmos.color = Color.yellow;
            //         }
            //
            //         Gizmos.DrawWireCube(e.position, itemRect.size);
            //     }
            //     return;
            // }
            //
            // if (m_Items.Count <= 0) return;
            //
            // Rect currentRect = GetStartRect();
            // for (var currentNode = m_Items.First;
            //      currentNode != null;
            //      currentNode = currentNode.Next
            //     )
            // {
            //     bool overlap = currentRect.Overlaps(ScrollRect.ViewportRect, false);
            //     if (!overlap)
            //     {
            //         Gizmos.color = Color.red;
            //     }
            //     else
            //     {
            //         Gizmos.color = Color.yellow;
            //     }
            //
            //     Gizmos.DrawWireCube(currentRect.position, currentRect.size);
            //     currentRect.x    += currentRect.size.x + xSpacing;
            // }
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

            LinkedListNode<IRectItem> currentNode = m_Items.First;

            Vector2 sizeDelta = ItemSizeDelta;
            sizeDelta = tr.TransformVector(sizeDelta);

            Rect currentRect = new Rect(
                tr.GetWorldCenterPosition() - new Vector3(rect.size.x * .5f, 0),
                sizeDelta);

            currentRect.x += padding.left;
            currentRect.x += currentRect.size.x * .5f;

            return currentRect;
        }

        public IEnumerator<IRectItem> GetEnumerator()
        {
            return ((IEnumerable<IRectItem>)m_Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}