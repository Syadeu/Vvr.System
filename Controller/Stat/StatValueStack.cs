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
using UnityEngine.Assertions;
using Vvr.System.Model;
using Vvr.UI.Observer;

namespace Vvr.System.Controller
{
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
            m_PushStats     =  StatValues.Copy(m_OriginalStats);
            m_ResultStats   =  StatValues.Copy(m_OriginalStats);

            ObjectObserver<StatValueStack>.Get(this).EnsureContainer();
        }

        public void Push(StatType t, float v)
        {
            m_PushStats    |= t;
            m_PushStats[t] += v;
            m_IsDirty      =  true;

            Update();
            $"[Stats:{m_Owner.Owner}:{m_Owner.GetInstanceID()}] Stat({t}) changed to {m_ResultStats[t]}".ToLog();
            ObjectObserver<StatValueStack>.ChangedEvent(this);
        }
        public void Push<TProcessor>(StatType t, float v) where TProcessor : struct, IStatValueProcessor
        {
            m_PushStats |= t;

            TProcessor processor = Activator.CreateInstance<TProcessor>();
            float processedV = processor.Process(m_PushStats, v);
            m_PushStats[t] += processedV;
            m_IsDirty = true;

            Update();
            $"[Stats:{m_Owner.Owner}:{m_Owner.GetInstanceID()}] Stat({t}) changed to {m_ResultStats[t]} with {VvrTypeHelper.TypeOf<TProcessor>.ToString()}".ToLog();
            ObjectObserver<StatValueStack>.ChangedEvent(this);
        }

        public void Update()
        {
            if (m_Modifiers.Any(x => x.IsDirty))
            {
                m_ModifiedStats |= m_OriginalStats.Types;
                foreach (var e in m_Modifiers.OrderBy(StatModifierComparer.Selector, StatModifierComparer.Static))
                {
                    e.UpdateValues(m_OriginalStats, ref m_ModifiedStats);
                }

                m_IsDirty = true;
            }

            if (m_IsDirty)
            {
                m_ResultStats |= m_ModifiedStats.Types | m_PushStats.Types;
                m_ResultStats.Clear();

                m_ResultStats += m_ModifiedStats;
                m_ResultStats += m_PushStats;
            }

            m_IsDirty = false;
        }
        public float GetValue(StatType t)
        {
            Update();
            return m_ResultStats[t];
        }

        public StatValueStack AddModifier(IStatModifier modifier)
        {
            Assert.IsFalse(m_Modifiers.Contains(modifier));
            m_Modifiers.Add(modifier);
            return this;
        }
        public void RemoveModifier(IStatModifier modifier)
        {
            m_Modifiers.Remove(modifier);
        }

        public void Dispose()
        {
            m_Modifiers.Clear();
            ObjectObserver<StatValueStack>.Remove(this);
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