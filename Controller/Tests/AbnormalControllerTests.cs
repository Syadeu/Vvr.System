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
// File created : 2024, 06, 25 22:06

#endregion

using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vvr.Controller.Abnormal;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.TestClass;

namespace Vvr.Controller.Tests
{
    [TestFixture]
    public sealed class AbnormalControllerTests
    {
        private AbnormalController m_Controller;

        private TestActor      Actor      { get; set; }
        private StatValueStack Stats      { get; set; }
        private IAbnormal      Controller => m_Controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Actor        = TestActor.Create(Owner.Issue);
            m_Controller = new AbnormalController(Actor);

            TimeController.Register(m_Controller);
        }
        [SetUp]
        public void SetUp()
        {
            TimeController.ResetTime();

            Stats = new StatValueStack(Actor, null);

            Stats.AddModifier(m_Controller);
            Stats.Update();
        }
        [TearDown]
        public void TearDown()
        {
            Stats.Dispose();
            m_Controller.Clear();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TimeController.Unregister(m_Controller);
            m_Controller.Dispose();
            Stats.Dispose();
            Actor.Dispose();
        }

        [Test]
        public async Task AddTest()
        {
            TestAbnormalData d = TestAbnormalData.Create();
            var handle = await Controller.AddAsync(d);

            Assert.IsTrue(Controller.Contains(handle));
            Assert.IsFalse(handle.Disposed);
        }

        [Test]
        public async Task RemoveTest()
        {
            TestAbnormalData d      = TestAbnormalData.Create();
            var              handle = await Controller.AddAsync(d);

            handle.Dispose();

            Assert.IsTrue(handle.Disposed);
            Assert.IsFalse(Controller.Contains(handle));
        }

        [Test, Repeat(10)]
        public void StatModifierTestMultiple()
        {
            StatModifierTestSingle().Wait();
        }

        [Test]
        public async Task StatModifierTestSingle()
        {
            TestAbnormalData d              = TestAbnormalData.Create(
                definition: TestAbnormalDefinition.Create(method: Method.Addictive));
            var              targetStatType = d.Definition.TargetStatus.ToStat();
            float            original       = Stats[targetStatType];

            $"original: {Stats}".ToLog();

            var handle = await Controller.AddAsync(d);

            Assert.IsTrue(m_Controller.IsDirty);
            if (handle.IsActivated)
                Assert.IsTrue((Stats.Types & targetStatType) == targetStatType);

            float current = Stats[targetStatType];
            $"current: {Stats}".ToLog();

            Assert.IsFalse(m_Controller.IsDirty);

            $"{Stats.OriginalStats}".ToLog();

            string str = $"Original: {original} Current: {current}\n"  +
                         $"Target: {targetStatType}, v: {d.Definition.Value} c: {Stats.Values.Count}";

            if (handle.IsActivated)
                Assert.IsFalse(TestUtils.Approximately(original, current), str);
            else
                Assert.IsTrue(TestUtils.Approximately(original, current), str);
        }

        [Test, Repeat(10)]
        public void MultipleStatModifierTest()
        {
            MultipleStatModifierTestTask().Wait();
        }

        private async Task MultipleStatModifierTestTask()
        {
            TestAbnormalData
                d0 = TestAbnormalData.Create(definition: TestAbnormalDefinition.Create(type: 0, method: Method.Addictive)),
                d1 = TestAbnormalData.Create(definition: TestAbnormalDefinition.Create(type: 1, method: Method.Addictive)),
                d2 = TestAbnormalData.Create(definition: TestAbnormalDefinition.Create(type: 2, method: Method.Addictive)),
                d3 = TestAbnormalData.Create(definition: TestAbnormalDefinition.Create(type: 3, method: Method.Addictive))
                ;

            var results = await UniTask.WhenAll(
                Controller.AddAsync(d0),
                Controller.AddAsync(d1),
                Controller.AddAsync(d2),
                Controller.AddAsync(d3)
            );

            int      activated        = 0;
            StatType expectedStatType = 0;
            if (results.Item1.IsActivated)
            {
                expectedStatType |= d0.Definition.TargetStatus.ToStat();
                activated++;
            }

            if (results.Item2.IsActivated)
            {
                expectedStatType |= d1.Definition.TargetStatus.ToStat();
                activated++;
            }

            if (results.Item3.IsActivated)
            {
                expectedStatType |= d2.Definition.TargetStatus.ToStat();
                activated++;
            }

            if (results.Item4.IsActivated)
            {
                expectedStatType |= d3.Definition.TargetStatus.ToStat();
                activated++;
            }

            Assert.AreEqual(expectedStatType, Stats.Types,
                $"Activated: {activated}\n"                                                         +
                $"Expected: {Convert.ToString((long)expectedStatType, 2)} " +
                $"But: {Convert.ToString((long)Stats.Types, 2)}");
        }
    }
}