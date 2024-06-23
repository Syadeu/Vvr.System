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
// File created : 2024, 06, 17 21:06
#endregion

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vvr.Model.Stat;
using Vvr.TestClass;

namespace Vvr.Controller.Stat.Tests
{
    [TestFixture]
    public class StatValueStackTests
    {
        #region Initialize

        private TestActor m_TestActor;

        private StatValues     m_OriginalStats;
        private StatValueStack m_StatValueStack;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestActor = new TestActor(OwnerHelper.Player, "Test Actor", "Test Actor");
        }
        [SetUp]
        public void SetUp()
        {
            m_OriginalStats   = GetOriginalStatValues();
            m_StatValueStack  = new StatValueStack(m_TestActor, m_OriginalStats);
            m_TestActor.Stats = m_StatValueStack;
        }

        [TearDown]
        public void TearDown()
        {
            m_TestActor.Stats = null;
            m_StatValueStack.Dispose();
        }

        protected virtual StatValues GetOriginalStatValues()
        {
            StatValues v = StatValues.Create(StatType.HP | StatType.ARM | StatType.ATT);
            v[StatType.HP]  = 10;
            v[StatType.ATT] = 1;
            v[StatType.ARM] = 5;

            return v;
        }

        private static StatValues CreateRandomStatValues()
        {
            long        t = 0b1111_1111 ^ (long)StatType.SHD;
            StatValues v = StatValues.Create((StatType)t);

            for (int i = 0; i < v.Values.Count; i++)
            {
                v.Values[i] = UnityEngine.Random.Range(short.MinValue, short.MaxValue);
            }

            return v;
        }

        protected static IEnumerable<StatValues> GetTestStatValues(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return CreateRandomStatValues();
            }
        }

        #endregion

        protected TestActor      Actor          => m_TestActor;
        protected StatValues     OriginalStats  => m_OriginalStats;
        protected StatValueStack StatValueStack => m_StatValueStack;

        protected void AreSame(StatType type, float v)
        {
            Assert.IsTrue(
                Mathf.Approximately(StatValueStack[type], v),
                $"Expected: {v} but {StatValueStack[type]}");
        }

        [Test]
        public void PushTest_0()
        {
            StatValueStack.Push(StatType.HP, 10);

            AreSame(StatType.HP, OriginalStats[StatType.HP] + 10);
        }
        [Test]
        public void PushTest_1()
        {
            DamageProcessor processor = new DamageProcessor();

            float v = processor.Process(OriginalStats, 10);

            StatValueStack.Push<DamageProcessor>(StatType.HP, 10);

            Debug.Log($"{OriginalStats[StatType.HP]} + {v}");
            Debug.Log(StatValueStack[StatType.HP]);

            AreSame(StatType.HP, OriginalStats[StatType.HP] + v);
        }

        [Test, TestCaseSource(typeof(StatValueStackTests), nameof(GetTestStatValues), new object[1]{ 5 })]
        public void ModifierTest_0(StatValues n)
        {
            TestStatModifier m = new TestStatModifier(n);

            StatValueStack.AddModifier(m);
            StatValueStack.Update();

            var expected = OriginalStats + n;
            Assert.AreEqual(expected.Values.Count, StatValueStack.Values.Count);
            Assert.AreEqual(expected.Types, StatValueStack.Types);

            for (int i = 0; i < StatValueStack.Values.Count; i++)
            {
                Assert.IsTrue(
                    Mathf.Approximately(expected.Values[i], StatValueStack.Values[i]),
                    $"{expected.Values[i]} == {StatValueStack.Values[i]}");
            }
        }
    }
}