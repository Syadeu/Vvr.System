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
// File created : 2024, 06, 22 18:06

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.TestClass
{
    [Serializable]
    public class TestStatValues : IRawStatValues
    {
        public static TestStatValues CreateRandom(int statCount = 3)
        {
            TestStatData[] d = new TestStatData[statCount];
            for (int i = 0; i < statCount; i++)
            {
                d[i] = TestStatData.CreateRandom();
            }

            return new TestStatValues(d);
        }

        [SerializeField]
        private TestStatData[] m_Stats;

        private bool       m_Initialized;
        private StatValues m_StatValues;

        public float this[StatType t]
        {
            get
            {
                Initialize();
                return m_StatValues[t];
            }
        }

        public StatType                 Types
        {
            get
            {
                Initialize();
                return m_StatValues.Types;
            }
        }

        public IReadOnlyList<float>     Values
        {
            get
            {
                Initialize();
                return (IReadOnlyList<float>)m_StatValues.Values;
            }
        }

        public IReadOnlyList<IStatData> RawData
        {
            get
            {
                Initialize();
                return m_Stats;
            }
        }

        public TestStatValues(params TestStatData[] stats)
        {
            m_Stats = stats;
        }

        private void Initialize()
        {
            if (m_Initialized) return;

            Dictionary<StatType, TestStatData> map   = new();
            StatType                           types = 0;
            for (int i = 0; i < m_Stats.Length; i++)
            {
                var s = m_Stats[i];
                types           |= s.StatType;
                map[s.StatType] =  s;
            }

            m_StatValues = StatValues.Create(types);
            foreach (var kvp in map)
            {
                m_StatValues.SetValue(kvp.Key, kvp.Value.Value);
            }

            m_Initialized = true;
        }

        public IEnumerator<KeyValuePair<StatType, float>> GetEnumerator()
        {
            return m_StatValues.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return m_StatValues.ToString();
        }
    }
}