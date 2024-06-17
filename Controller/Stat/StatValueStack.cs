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
// File created : 2024, 05, 07 01:05

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Model.Stat;
using Vvr.UI.Observer;

namespace Vvr.Controller.Stat
{
    [PublicAPI]
    public sealed class StatValueStack : IStatValueStack, IDisposable
    {
        private readonly IActor m_Owner;

        private readonly IReadOnlyStatValues m_OriginalStats;
        private          StatValues          m_ModifiedStats;
        private          StatValues          m_PushStats;
        private          StatValues          m_ResultStats;

        private readonly List<IStatModifier> m_Modifiers = new();

        private bool m_IsDirty;

        public float this[StatType t] => GetValue(t);
        public StatType             Types
        {
            get
            {
                Update();
                return m_ResultStats.Types;
            }
        }

        public IReadOnlyList<float> Values
        {
            get
            {
                Update();
                return ((IReadOnlyStatValues)m_ResultStats).Values;
            }
        }

        public IReadOnlyStatValues OriginalStats => m_OriginalStats;

        public StatValueStack(IActor owner, IReadOnlyStatValues originalStats)
        {
            m_Owner         =  owner;
            m_OriginalStats =  originalStats;
            m_ModifiedStats |= m_OriginalStats.Types;
            m_PushStats     |= m_OriginalStats.Types;
            m_ResultStats   |= m_OriginalStats.Types;
        }

        public void Push(StatType t, float v)
        {
            using var debugTimer = DebugTimer.Start();
            m_PushStats    |= t;
            m_PushStats[t] += v;
            m_IsDirty      =  true;

            Update();
            $"[Stats:{m_Owner.Owner}:{m_Owner.GetInstanceID()}] Stat({t}) changed to {m_ResultStats[t]}".ToLog();
        }
        public void Push<TProcessor>(StatType t, float v) where TProcessor : struct, IStatValueProcessor
        {
            using var debugTimer = DebugTimer.Start();
            m_PushStats |= t;

            TProcessor processor = Activator.CreateInstance<TProcessor>();
            float processedV = processor.Process(m_ResultStats, v);

            m_PushStats[t] += processedV;
            m_IsDirty = true;

            Update();
            $"[Stats:{m_Owner.Owner}:{m_Owner.GetInstanceID()}] Stat({t}) changed to {m_ResultStats[t]}(processed: {processedV}) with {VvrTypeHelper.TypeOf<TProcessor>.ToString()}".ToLog();
        }

        public void Update()
        {
            using var debugTimer = DebugTimer.Start();

            if (!m_Modifiers.Any(x => x.IsDirty) && !m_IsDirty) return;

            m_ModifiedStats |= m_OriginalStats.Types;
            m_ModifiedStats.Clear();
            m_ModifiedStats += m_OriginalStats;

            float prevHp = m_ModifiedStats[StatType.HP];
            float currentHp;
            foreach (var e in m_Modifiers.OrderBy(StatModifierComparer.Selector, StatModifierComparer.Static))
            {
                e.UpdateValues(m_OriginalStats, ref m_ModifiedStats);

                currentHp = m_ModifiedStats[StatType.HP];
                if (currentHp < prevHp && (m_ModifiedStats.Types & StatType.SHD) == StatType.SHD)
                {
                    float shield = m_ModifiedStats[StatType.SHD];
                    float sub    = prevHp - currentHp;
                    if (sub > shield)
                    {
                        currentHp                     -= sub - shield;
                        m_ModifiedStats[StatType.HP]  =  currentHp;
                        m_ModifiedStats[StatType.SHD] =  0;
                    }
                    else
                    {
                        m_ModifiedStats[StatType.SHD] = shield - sub;
                    }
                }

                prevHp = currentHp;
            }

            m_ResultStats |= m_ModifiedStats.Types | m_PushStats.Types;
            m_ResultStats.Clear();

            m_ResultStats += m_ModifiedStats;

            prevHp        =  m_ResultStats[StatType.HP];
            m_ResultStats += m_PushStats;

            currentHp = m_ResultStats[StatType.HP];
            if (currentHp < prevHp && (m_ResultStats.Types & StatType.SHD) == StatType.SHD)
            {
                float shield = m_ResultStats[StatType.SHD];
                float sub    = prevHp - currentHp;
                if (sub > shield)
                {
                    currentHp                   -= sub - shield;
                    m_ResultStats[StatType.HP]  =  currentHp;
                    m_ResultStats[StatType.SHD] =  0;
                }
                else
                {
                    m_ResultStats[StatType.HP]  = prevHp;
                    m_ResultStats[StatType.SHD] = shield - sub;
                }
            }

            m_IsDirty = false;
        }

        public float GetValue(StatType t)
        {
            Update();
            return m_ResultStats[t];
        }

        public IStatValueStack AddModifier(IStatModifier modifier)
        {
            const string debugName  = "StatValueStack.AddModifier";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            Assert.IsFalse(m_Modifiers.Contains(modifier));
            m_Modifiers.Add(modifier);
            return this;
        }
        public IStatValueStack RemoveModifier(IStatModifier modifier)
        {
            const string debugName  = "StatValueStack.RemoveModifier";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            m_Modifiers.Remove(modifier);
            return this;
        }

        public void Dispose()
        {
            m_Modifiers.Clear();
        }

        public IEnumerator<KeyValuePair<StatType, float>> GetEnumerator()
        {
            return m_ResultStats.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}