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
// File created : 2024, 06, 13 16:06

#endregion

using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.UComponent.UI
{
    [RequireComponent(typeof(IRectLaneContainer))]
    [PublicAPI, HideMonoScript]
    public class MultiLaneController : MonoBehaviour
    {
        private IRectLaneContainer m_LaneContainer;

        public IRectLaneContainer LaneContainer => m_LaneContainer ??= GetComponent<IRectLaneContainer>();

        public int ItemCount => LaneContainer.GetEnumerable().Sum(x => x.Count);

        public virtual void Add(IScrollRectItem item)
        {
            int count = ItemCount;
            int x     = count % LaneContainer.Count;

            LaneContainer[x].Add(item);
        }
        public virtual void Remove(IScrollRectItem item)
        {
            foreach (var lane in LaneContainer.GetEnumerable())
            {
                if (!lane.Remove(item)) continue;

                break;
            }
        }

        public void Clear()
        {
            foreach (var lane in LaneContainer.GetEnumerable())
            {
                lane.Clear();
            }
        }
    }
}