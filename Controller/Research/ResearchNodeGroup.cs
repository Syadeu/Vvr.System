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
// File created : 2024, 05, 23 11:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Controller.Research
{
    public sealed class ResearchNodeGroup
    {
        private sealed class ResearchNode : IResearchNode, IMethodArgumentResolver
        {
            private readonly ResearchSheet.Row m_Data;

            private ResearchNode[] m_Children;
            private bool           m_IsDirty;

            private readonly CustomMethodDelegate
                m_CalculateResearchTime,
                m_CalculateConsumableCount,
                m_CalculateStatModifier;

            private readonly StatValueGetterDelegate m_StatGetter;
            private readonly StatValueSetterDelegate m_StatSetter;

            public string        Id     => m_Data.Id;
            public IResearchNode Parent { get; private set; }

            IReadOnlyList<IResearchNode> IResearchNode.Children => m_Children;

            public int Level    { get; private set; }
            public int MaxLevel => m_Data.Definition.MaxLevel;

            public TimeSpan NextLevelResearchTime
            {
                get
                {
                    if (Level < MaxLevel)
                    {
                        float v = m_CalculateResearchTime(this);
                        return TimeSpan.FromSeconds(v);
                    }
                    return TimeSpan.Zero;
                }
            }
            public float NextLevelRequired
            {
                get
                {
                    if (Level < MaxLevel)
                    {
                        return m_CalculateConsumableCount(this);
                    }

                    return 0;
                }
            }

            bool IStatModifier.IsDirty => m_IsDirty;
            int IStatModifier. Order   => StatModifierOrder.Item - 1;

            public ResearchNode(ResearchSheet.Row data)
            {
                m_Data = data;

                var statType = StatProvider.Static[data.Definition.TargetStat.Id];
                m_StatGetter = StatValues.GetGetMethod(statType);
                m_StatSetter = StatValues.GetSetMethod(statType);

                m_CalculateResearchTime    = CustomMethod.Static[data.Methods.ResearchTime.Ref];
                m_CalculateConsumableCount = CustomMethod.Static[data.Methods.Consumable.Ref];
                m_CalculateStatModifier    = CustomMethod.Static[data.Methods.StatModifier.Ref];
            }

            public void Build(IReadOnlyDictionary<string, ResearchNode> map)
            {
                m_Children = new ResearchNode[m_Data.Connection.Count];
                int i = 0;
                foreach (var childData in m_Data.Connection.Select(x => x.Ref).OrderBy(x => x.Definition.Order))
                {
                    var child = map[childData.Id];
                    child.Parent    = this;
                    m_Children[i++] = child;
                }
            }

            private IReadOnlyStatValues m_CachedStatValues;
            void IStatModifier.UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
            {
                m_CachedStatValues = stats;
                float v = m_CalculateStatModifier(this);
                m_CachedStatValues = null;

                m_StatSetter(ref stats, v);

                m_IsDirty = false;
            }

            float IMethodArgumentResolver.Resolve(string arg)
            {
                if (CustomMethodArgumentNames.TARGET == arg)
                {
                    Assert.IsNotNull(m_CachedStatValues);
                    return m_StatGetter(m_CachedStatValues);
                }
                if (CustomMethodArgumentNames.LVL    == arg) return Level;
                if (CustomMethodArgumentNames.MAXLVL == arg) return MaxLevel;

                throw new ArgumentException($"{arg} is not valid argument", nameof(arg));
            }
        }

        public static ResearchNodeGroup Build(IEnumerable<ResearchSheet.Row> nodes)
        {
            Dictionary<string, ResearchNode> map = nodes.ToDictionary(
                x => x.Id, x => new ResearchNode(x));

            var result = new IResearchNode[map.Count];
            foreach (var node in map.Values)
            {
                node.Build(map);
            }

            IResearchNode        root  = map.Values.First(x => x.Parent == null);
            Queue<IResearchNode> queue = new();
            queue.Enqueue(root);

            int i = 0;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                result[i++] = c;

                foreach (var t in c.Children)
                {
                    queue.Enqueue(t);
                }
            }

            return new(result);
        }

        private readonly IResearchNode[] m_Nodes;

        private ResearchNodeGroup(IResearchNode[] nodes)
        {
            m_Nodes = nodes;
        }

        public void Connect(IStatValueStack stat)
        {
            using var debugTimer = DebugTimer.Start();

            foreach (var node in m_Nodes)
            {
                stat.AddModifier(node);
            }
        }

        public void Disconnect(IStatValueStack stat)
        {
            using var debugTimer = DebugTimer.Start();
            
            foreach (var node in m_Nodes)
            {
                stat.RemoveModifier(node);
            }
        }
    }
}