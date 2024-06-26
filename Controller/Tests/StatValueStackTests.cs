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
using Vvr.Controller.Stat;
using Vvr.Model.Stat;
using Vvr.TestClass;

namespace Vvr.Controller.Tests
{
    [TestFixture]
    public sealed class StatValueStackTests
    {
        #region Initialize

        private TestActor m_TestActor;

        private StatValueStack m_StatValueStack;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestActor = new TestActor(OwnerHelper.Player, "Test Actor", "Test Actor");
        }
        [SetUp]
        public void SetUp()
        {
            m_StatValueStack  = new StatValueStack(m_TestActor, null);
            m_TestActor.Stats = m_StatValueStack;
        }

        [TearDown]
        public void TearDown()
        {
            m_TestActor.Stats = null;
            m_StatValueStack.Dispose();
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

        private TestActor      Actor          => m_TestActor;
        private StatValueStack StatValueStack => m_StatValueStack;

        private void AreSame(StatType type, float v)
        {
            Assert.IsTrue(
                Mathf.Approximately(StatValueStack[type], v),
                $"Expected: {v} but {StatValueStack[type]}");
        }

        struct Processor_0 : IStatValueProcessor
        {
            public float Process(in IReadOnlyStatValues stats, in StatType type, float value)
            {
                return value;
            }
        }
        struct Processor_1 : IStatValueProcessor
        {
            public float Process(in IReadOnlyStatValues stats, in StatType type, float value)
            {
                return -value;
            }
        }

        [Test]
        public void PushTest_0()
        {
            StatType targetType = (StatType)(1L << 30);

            StatValueStack.Push<Processor_0>(targetType, 100);

            AreSame(targetType, 100);
        }

        [Test]
        public void PushTest_1()
        {
            StatValueStack.Push(StatType.HP, 10);

            AreSame(StatType.HP, 10);
        }
        [Test]
        public void PushTest_2()
        {
            DamageProcessor processor = new DamageProcessor();

            float v = processor.Process(StatValueStack, StatType.HP, 10);

            StatValueStack.Push<DamageProcessor>(StatType.HP, 10);

            AreSame(StatType.HP, v);
        }

        [Test]
        public void PushTest_3()
        {
            StatType targetType = (StatType)(1L << 30);

            StatValueStack.Push<Processor_0>(targetType, 100);
            StatValueStack.Push<Processor_0>(targetType, 100);
            StatValueStack.Push<Processor_0>(targetType, 100);
            StatValueStack.Push<Processor_0>(targetType, 100);
            StatValueStack.Push<Processor_0>(targetType, 100);

            AreSame(targetType, 500);
        }
        [Test]
        public void PushTest_4()
        {
            StatType targetType = (StatType)(1L << 30);

            StatValueStack.Push<Processor_0>(targetType, 100);
            AreSame(targetType, 100);
            StatValueStack.Push<Processor_1>(targetType, 100);
            AreSame(targetType, 0);
            StatValueStack.Push<Processor_0>(targetType, 100);
            AreSame(targetType, 100);
        }

        [Test, TestCaseSource(typeof(StatValueStackTests), nameof(GetTestStatValues), new object[1]{ 5 })]
        public void ModifierTest_0(StatValues n)
        {
            TestStatModifier m = new TestStatModifier(n);

            StatValueStack.AddModifier(m);
            StatValueStack.Update();

            var expected = n;
            Assert.AreEqual(expected.Values.Count, StatValueStack.Values.Count);
            Assert.AreEqual(expected.Types, StatValueStack.Types);

            for (int i = 0; i < StatValueStack.Values.Count; i++)
            {
                Assert.IsTrue(
                    Mathf.Approximately(expected.Values[i], StatValueStack.Values[i]),
                    $"{expected.Values[i]} == {StatValueStack.Values[i]}");
            }
        }
        [Test]
        public void ModifierTest_1()
        {
            IStatModifier
                hpPlus = new TestStatValueModifier(0, StatType.HP, 100),
                shdPlus = new TestStatValueModifier(1, StatType.SHD, 100),
                hpMinus = new TestStatValueModifier(2, StatType.HP, -50)
                ;

            StatValueStack.AddModifier(hpPlus);
            AreSame(StatType.HP, 100);
            StatValueStack.AddModifier(hpMinus);
            AreSame(StatType.HP, 50);
            StatValueStack.AddModifier(shdPlus);
            AreSame(StatType.HP, 50);
            AreSame(StatType.SHD, 100);
        }
        [Test]
        public void ModifierTest_2()
        {
            IStatModifier
                m0 = new TestStatValueModifier(0, StatType.HP, 100),
                m1 = new TestStatValueModifier(1, StatType.HP, -50),
                m2 = new TestStatValueMulModifier(2, StatType.HP, 2)
                ;

            StatValueStack.AddModifier(m0);
            AreSame(StatType.HP, 100);
            StatValueStack.AddModifier(m2);
            AreSame(StatType.HP, 200);
            StatValueStack.AddModifier(m1);
            AreSame(StatType.HP, 100);
        }
        [Test]
        public void ModifierTest_3()
        {
            IStatModifier
                m0 = new TestStatValueModifier(0, StatType.HP, 100),
                m1 = new TestStatValueModifier(1, StatType.HP, -50),
                m2 = new TestStatValueMulModifier(2, StatType.HP, 2)
                ;

            StatValueStack.AddModifier(m0);
            StatValueStack.AddModifier(m2);
            StatValueStack.AddModifier(m1);

            StatValueStack.RemoveModifier(m0);
            AreSame(StatType.HP, -100);
        }

        [Test]
        public void PostProcessorTest_0()
        {
            HpShieldPostProcessor m = new HpShieldPostProcessor();
            StatValueStack.AddPostProcessor(m);

            StatValueStack.Push(StatType.HP, 100);

            AreSame(StatType.HP, 100);

            StatValueStack.Push(StatType.SHD, 100);

            AreSame(StatType.HP, 100);
            AreSame(StatType.SHD, 100);

            StatValueStack.Push(StatType.HP, -100);

            AreSame(StatType.HP, 100);
            AreSame(StatType.SHD, 0);
        }

        [Test]
        public void PostProcessorTest_1()
        {
            IStatModifier
                hpPlus  = new TestStatValueModifier(0, StatType.HP, 100),
                shdPlus = new TestStatValueModifier(1, StatType.SHD, 100),
                hpMinus = new TestStatValueModifier(2, StatType.HP, -50)
                ;

            HpShieldPostProcessor m = new HpShieldPostProcessor();
            StatValueStack.AddPostProcessor(m);

            StatValueStack.AddModifier(hpPlus);
            StatValueStack.AddModifier(shdPlus);
            StatValueStack.AddModifier(hpMinus);

            AreSame(StatType.HP, 100);
            AreSame(StatType.SHD, 50);
        }
    }
}