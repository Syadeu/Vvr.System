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
    public sealed class ResearchNodeGroup : IResearchNodeGroup
    {
        private sealed class ResearchNode : IResearchNode, IMethodArgumentResolver, IDisposable
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

            private bool m_Disposed;

            public string        Id     => m_Data.Id;
            public IResearchNode Parent { get; private set; }

            public IReadOnlyList<ResearchNode> Children => m_Children;
            IReadOnlyList<IResearchNode> IResearchNode.Children => m_Children;

            public int Level    { get; private set; }
            public int MaxLevel => m_Data.Definition.MaxLevel;

            public TimeSpan NextLevelResearchTime
            {
                get
                {
                    if (m_Disposed)
                        throw new ObjectDisposedException(nameof(IResearchNode));

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
                    if (m_Disposed)
                        throw new ObjectDisposedException(nameof(IResearchNode));

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
                if (m_Data.Connection is not { Count: > 0 })
                {
                    m_Children = Array.Empty<ResearchNode>();
                    return;
                }
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
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(IResearchNode));

                m_CachedStatValues = stats;
                float v = m_CalculateStatModifier(this);
                m_CachedStatValues = null;

                m_StatSetter(ref stats, v);

                m_IsDirty = false;
            }

            float IMethodArgumentResolver.Resolve(string arg)
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(IResearchNode));

                if (CustomMethodArgumentNames.TARGET == arg)
                {
                    Assert.IsNotNull(m_CachedStatValues);
                    return m_StatGetter(m_CachedStatValues);
                }
                if (CustomMethodArgumentNames.LVL    == arg) return Level;
                if (CustomMethodArgumentNames.MAXLVL == arg) return MaxLevel;

                throw new ArgumentException($"{arg} is not valid argument", nameof(arg));
            }

            public void Dispose()
            {
                Array.Clear(m_Children, 0, m_Children.Length);
                m_Children = null;

                m_Disposed = true;
            }
        }

        public static IResearchNodeGroup Build(IEnumerable<ResearchSheet.Row> nodes)
        {
            Dictionary<string, ResearchNode> map = nodes.ToDictionary(
                x => x.Id, x => new ResearchNode(x));

            var result = new ResearchNode[map.Count];
            foreach (var node in map.Values)
            {
                node.Build(map);
            }

            ResearchNode        root  = map.Values.First(x => x.Parent == null);
            Queue<ResearchNode> queue = new();
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

            return new ResearchNodeGroup(result);
        }

        private readonly ResearchNode[] m_Nodes;

        public IResearchNode Root
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(IResearchNode));

                return m_Nodes[0];
            }
        }

        public IResearchNode this[int i]
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(IResearchNode));

                return m_Nodes[i];
            }
        }

        public bool Disposed { get; private set; }

        private ResearchNodeGroup(ResearchNode[] nodes)
        {
            m_Nodes = nodes;
        }

        public void Connect(IStatValueStack stat)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(IResearchNode));

            using var debugTimer = DebugTimer.Start();

            foreach (var node in m_Nodes)
            {
                stat.AddModifier(node);
            }
        }

        public void Disconnect(IStatValueStack stat)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(IResearchNode));

            using var debugTimer = DebugTimer.Start();

            foreach (var node in m_Nodes)
            {
                stat.RemoveModifier(node);
            }
        }

        public void Dispose()
        {
            foreach (var item in m_Nodes)
            {
                item.Dispose();
            }

            Array.Clear(m_Nodes, 0, m_Nodes.Length);

            Disposed = true;
        }
    }
}