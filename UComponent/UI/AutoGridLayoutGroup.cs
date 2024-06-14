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
// File created : 2024, 06, 14 11:06

#endregion

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Vvr.UComponent.UI
{
    [DisallowMultipleComponent]
    [HideMonoScript]
    public class AutoGridLayoutGroup : LayoutGroup
    {
        public enum Constraint
        {
            /// <summary>
            /// Constrain the number of columns to a specified number.
            /// </summary>
            FixedColumnCount = 0,

            /// <summary>
            /// Constraint the number of rows to a specified number.
            /// </summary>
            FixedRowCount = 1
        }

        [SerializeField] protected GridLayoutGroup.Corner m_StartCorner = GridLayoutGroup.Corner.UpperLeft;
        [SerializeField] protected GridLayoutGroup.Axis   m_StartAxis   = GridLayoutGroup.Axis.Horizontal;
        [SerializeField] protected Vector2                m_Spacing     = Vector2.zero;

        [Space]
        [SerializeField] protected Constraint m_Constraint      = Constraint.FixedColumnCount;

        [SerializeField]
        protected int m_ConstraintCount = 2;


        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            float preferred = LayoutUtility.GetPreferredSize(rectTransform, 0);
            SetLayoutInputForAxis(
                0,
                preferred,
                -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float preferred = LayoutUtility.GetPreferredSize(rectTransform, 1);
            SetLayoutInputForAxis(
                0,
                preferred,
                -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }

        private void SetCellsAlongAxis(int axis)
        {
            Vector2 sizeDelta     = rectTransform.rect.size;

            // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
            // and only vertical values when invoked for the vertical axis.
            // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
            // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
            // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.
            var rectChildrenCount = rectChildren.Count;

            int cellCountX = 1;
            int cellCountY = 1;
            if (m_Constraint == Constraint.FixedColumnCount)
            {
                cellCountX = m_ConstraintCount;

                if (rectChildrenCount > cellCountX)
                    cellCountY = rectChildrenCount / cellCountX + (rectChildrenCount % cellCountX > 0 ? 1 : 0);
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                cellCountY = m_ConstraintCount;

                if (rectChildrenCount > cellCountY)
                    cellCountX = rectChildrenCount / cellCountY + (rectChildrenCount % cellCountY > 0 ? 1 : 0);
            }

            int cornerX = (int)m_StartCorner % 2;
            int cornerY = (int)m_StartCorner / 2;

            int cellsPerMainAxis, actualCellCountX, actualCellCountY;
            if (m_StartAxis == GridLayoutGroup.Axis.Horizontal)
            {
                cellsPerMainAxis = cellCountX;
                actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildrenCount);

                if (m_Constraint == Constraint.FixedRowCount)
                    actualCellCountY = Mathf.Min(cellCountY, rectChildrenCount);
                else
                    actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
            }
            else
            {
                cellsPerMainAxis = cellCountY;
                actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildrenCount);

                if (m_Constraint == Constraint.FixedColumnCount)
                    actualCellCountX = Mathf.Min(cellCountX, rectChildrenCount);
                else
                    actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
            }

            var cellSizeDelta = new Vector2(
                (sizeDelta.x - m_Padding.horizontal) / actualCellCountX - m_Spacing.x * (actualCellCountX - 1),
                (sizeDelta.y - m_Padding.vertical) / actualCellCountY - m_Spacing.y * (actualCellCountY - 1)
            );

            if (axis == 0)
            {
                // Only set the sizes when invoked for horizontal axis, not the positions.

                for (int i = 0; i < rectChildrenCount; i++)
                {
                    RectTransform rect = rectChildren[i];

                    m_Tracker.Add(this, rect,
                        DrivenTransformProperties.Anchors          |
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.SizeDelta);

                    rect.anchorMin = Vector2.up;
                    rect.anchorMax = Vector2.up;
                    rect.sizeDelta = cellSizeDelta;
                }

                return;
            }

            Vector2 requiredSpace = new Vector2(
                actualCellCountX * cellSizeDelta.x + (actualCellCountX - 1) * m_Spacing.x,
                actualCellCountY * cellSizeDelta.y + (actualCellCountY - 1) * m_Spacing.y
            );
            Vector2 startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
            );

            // Fixes case 1345471 - Makes sure the constraint column / row amount is always respected
            int childrenToMove = 0;
            if (rectChildrenCount > m_ConstraintCount && Mathf.CeilToInt((float)rectChildrenCount / (float)cellsPerMainAxis) < m_ConstraintCount)
            {
                childrenToMove = m_ConstraintCount - Mathf.CeilToInt((float)rectChildrenCount / (float)cellsPerMainAxis);
                childrenToMove += Mathf.FloorToInt((float)childrenToMove / ((float)cellsPerMainAxis - 1));
                if (rectChildrenCount % cellsPerMainAxis == 1)
                    childrenToMove += 1;
            }

            for (int i = 0; i < rectChildrenCount; i++)
            {
                int positionX;
                int positionY;
                if (m_StartAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    if (m_Constraint == Constraint.FixedRowCount && rectChildrenCount - i <= childrenToMove)
                    {
                        positionX = 0;
                        positionY = m_ConstraintCount - (rectChildrenCount - i);
                    }
                    else
                    {
                        positionX = i % cellsPerMainAxis;
                        positionY = i / cellsPerMainAxis;
                    }
                }
                else
                {
                    if (m_Constraint == Constraint.FixedColumnCount && rectChildrenCount - i <= childrenToMove)
                    {
                        positionX = m_ConstraintCount - (rectChildrenCount - i);
                        positionY = 0;
                    }
                    else
                    {
                        positionX = i / cellsPerMainAxis;
                        positionY = i % cellsPerMainAxis;
                    }
                }

                if (cornerX == 1)
                    positionX = actualCellCountX - 1 - positionX;
                if (cornerY == 1)
                    positionY = actualCellCountY - 1 - positionY;

                SetChildAlongAxis(rectChildren[i], 0, startOffset.x + (cellSizeDelta[0] + m_Spacing[0]) * positionX, cellSizeDelta[0]);
                SetChildAlongAxis(rectChildren[i], 1, startOffset.y + (cellSizeDelta[1] + m_Spacing[1]) * positionY, cellSizeDelta[1]);
            }
        }
    }
}