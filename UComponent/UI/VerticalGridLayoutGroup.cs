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
// File created : 2024, 06, 18 19:06
#endregion

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Vvr.UComponent.UI
{
    [PublicAPI]
    public class VerticalGridLayoutGroup : LayoutGroup
    {
        sealed class RectGroup : IRectGroup
        {
            private readonly List<RectTransform> m_List = new();

            public int                  Group { get; }
            public IList<RectTransform> List  => m_List;

            public RectGroup(int group)
            {
                Group = group;
            }
        }

        private readonly Dictionary<int, RectGroup> m_Groups = new();

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            foreach (var e in m_Groups.Values)
            {
                e.List.Clear();
            }
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var e = rectChildren[i];

                int groupId = 0;
                if (e.TryGetComponent(out IRectGroup g))
                    groupId = g.Group;

                if (!m_Groups.TryGetValue(groupId, out var entry))
                {
                    entry             = new RectGroup(groupId);
                    m_Groups[groupId] = entry;
                }

                entry.List.Add(e);
            }


        }

        public override void CalculateLayoutInputVertical()
        {
            throw new System.NotImplementedException();
        }

        public override void SetLayoutHorizontal()
        {
            throw new System.NotImplementedException();
        }

        public override void SetLayoutVertical()
        {
            throw new System.NotImplementedException();
        }
    }

    [PublicAPI]
    public interface IRectGroup
    {
        int Group { get; }
    }
}