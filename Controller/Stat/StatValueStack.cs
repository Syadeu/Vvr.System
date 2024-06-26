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
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.UI.Observer;

namespace Vvr.Controller.Stat
{
    [PublicAPI]
    public sealed class StatValueStack : IStatValueStack, IDisposable
    {
        private readonly IActor m_Owner;

        [CanBeNull]
        private readonly IReadOnlyStatValues m_OriginalStats;
        private          StatValues          m_ModifiedStats;
        private          StatValues          m_PushStats;
        private          StatValues          m_ResultStats;

        // These lists are ordered list
        private readonly List<IStatModifier>      m_Modifiers      = new();
        private readonly List<IStatPostProcessor> m_PostProcessors = new();

        private bool m_IsDirty;

        public float this[StatType t] => GetValue(t);
        public StatType             Types
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(StatValueStack));

                Update();
                return m_ResultStats.Types;
            }
        }

        public IReadOnlyList<float> Values
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(StatValueStack));

                Update();
                if (m_ResultStats.Values is not null)
                    return (IReadOnlyList<float>)m_ResultStats.Values;

                return Array.Empty<float>();
            }
        }

        public bool Disposed { get; private set; }

        [CanBeNull]
        public IReadOnlyStatValues OriginalStats
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(StatValueStack));
                return m_OriginalStats;
            }
        }

        public StatValueStack([NotNull] IActor owner, [CanBeNull] IReadOnlyStatValues originalStats)
        {
            m_Owner         =  owner;
            m_OriginalStats =  originalStats;
            if (originalStats is not null)
            {
                m_ModifiedStats |= m_OriginalStats.Types;
                m_PushStats     |= m_OriginalStats.Types;

                m_ResultStats = StatValues.Copy(m_OriginalStats);
            }
        }

        public void Push(StatType t, float v)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(StatValueStack));

            using var debugTimer = DebugTimer.Start();
            m_PushStats |= t;

            using (var tempArray = TempArray<float>.Shared(m_PushStats.Values.Count))
            {
                m_PushStats.Values.CopyTo(tempArray.Value);
                StatValues prevStats = StatValues.Create(m_PushStats.Types, tempArray.Value);

                m_PushStats[t] += v;

                PostProcess(in prevStats, ref m_PushStats);
            }

            m_IsDirty = true;

            // Update();
            // $"[Stats:{m_Owner.Owner}:{m_Owner.GetInstanceID()}] Stat({t}) changed to {m_ResultStats[t]}".ToLog();
        }
        public void Push<TProcessor>(StatType t, float v) where TProcessor : struct, IStatValueProcessor
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(StatValueStack));

            using var debugTimer = DebugTimer.Start();
            m_PushStats |= t;

            TProcessor processor  = default(TProcessor);
            float      processedV = processor.Process(m_ResultStats, t, v);

            m_PushStats[t] += processedV;
            m_IsDirty = true;

            // Update();
            // $"[Stats:{m_Owner.Owner}:{m_Owner.GetInstanceID()}] Stat({t}) changed to {m_ResultStats[t]}(processed: {processedV}) with {VvrTypeHelper.TypeOf<TProcessor>.ToString()}".ToLog();
        }

        public void Update()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(StatValueStack));

            using var debugTimer = DebugTimer.Start();

            if (!m_Modifiers.Any(x => x.IsDirty) && !m_IsDirty) return;

            if (m_OriginalStats is not null)
                m_ModifiedStats |= m_OriginalStats.Types;

            m_ModifiedStats.Clear();
            m_ModifiedStats += m_OriginalStats;
            m_ResultStats   |= m_ModifiedStats.Types | m_PushStats.Types;
            m_ResultStats.Clear();

            // float prevHp = m_ModifiedStats[StatType.HP];
            for (int i = 0; i < m_Modifiers.Count; i++)
            {
                var e = m_Modifiers[i];

                using (var tempValueArray = TempArray<float>.Shared(m_ModifiedStats.Values?.Count ?? 0))
                {
                    var prevStatTypes = m_ModifiedStats.Types;
                    if (m_ModifiedStats.Values is not null)
                        m_ModifiedStats.Values.CopyTo(tempValueArray.Value);

                    e.UpdateValues(m_OriginalStats, ref m_ModifiedStats);

                    StatValues prevStats = StatValues.Create(prevStatTypes, tempValueArray.Value);
                    PostProcess(in prevStats, ref m_ModifiedStats);
                }
            }

            m_ResultStats += m_ModifiedStats;

            PostUpdate();

            m_IsDirty = false;
        }

        private void PostProcess(in StatValues previous, ref StatValues current)
        {
            for (int i = 0; i < m_PostProcessors.Count; i++)
            {
                var e = m_PostProcessors[i];

                e.OnChanged(in previous, ref current);
            }
        }

        private void PostUpdate()
        {
            // float prevHp = m_ResultStats[StatType.HP];

            using (var tempValueArray = TempArray<float>.Shared(m_ResultStats.Values.Count))
            {
                var prevStatTypes = m_ResultStats.Types;
                m_ResultStats.Values.CopyTo(tempValueArray.Value);

                m_ResultStats += m_PushStats;

                StatValues prevStats = StatValues.Create(prevStatTypes, tempValueArray.Value);
                PostProcess(in prevStats, ref m_ResultStats);
            }
        }

        public float GetValue(StatType t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(StatValueStack));

            Update();
            return m_ResultStats[t];
        }

        public IStatValueStack AddModifier(IStatModifier modifier)
        {
            const string debugName  = "StatValueStack.AddModifier";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            Assert.IsFalse(m_Modifiers.Contains(modifier));
            m_Modifiers.Add(modifier, StatModifierComparer.Static);
            m_IsDirty = true;
            return this;
        }
        public IStatValueStack RemoveModifier(IStatModifier modifier)
        {
            const string debugName  = "StatValueStack.RemoveModifier";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            m_Modifiers.Remove(modifier, StatModifierComparer.Static);
            m_IsDirty = true;
            return this;
        }

        public IStatValueStack AddPostProcessor(IStatPostProcessor processor)
        {
            Assert.IsFalse(m_PostProcessors.Contains(processor));

            m_PostProcessors.Add(processor, StatPostProcessorComparer.Static);
            return this;
        }
        public IStatValueStack RemovePostProcessor(IStatPostProcessor processor)
        {
            Assert.IsFalse(m_PostProcessors.Contains(processor));

            m_PostProcessors.Remove(processor, StatPostProcessorComparer.Static);
            return this;
        }

        public void Clear()
        {
            m_PostProcessors.Clear();
            m_Modifiers.Clear();
            m_IsDirty = true;
        }
        public void Dispose()
        {
            m_PostProcessors.Clear();
            m_Modifiers.Clear();
            Disposed = true;
        }

        public IEnumerator<KeyValuePair<StatType, float>> GetEnumerator()
        {
            return m_ResultStats.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return m_ResultStats.ToString();
        }
    }
}