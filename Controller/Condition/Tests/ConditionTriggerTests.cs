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
// File created : 2024, 06, 19 19:06
#endregion

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vvr.Provider;
using Vvr.TestClass;

namespace Vvr.Controller.Condition.Tests
{
    [TestFixture]
    public class ConditionTriggerTests
    {
        public struct EventContext
        {
            public IEventTarget    eventTarget;
            public Model.Condition condition;
            public string          value;
        }

        private TestEventTarget               TestEventTarget { get; } = new(nameof(ConditionTriggerTests));
        private ConcurrentQueue<EventContext> EventQueue      { get; } = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ConditionTrigger.OnEventExecutedAsync += OnEventExecutedAsync;
        }
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ConditionTrigger.OnEventExecutedAsync -= OnEventExecutedAsync;
        }
        [TearDown]
        public void TearDown()
        {
            EventQueue.Clear();
        }

        private UniTask OnEventExecutedAsync(IEventTarget e, Model.Condition condition,
            string                                                                value)
        {
            EventQueue.Enqueue(new EventContext()
            {
                eventTarget = e,
                condition = condition,
                value = value
            });
            return UniTask.CompletedTask;
        }

        [Test]
        public async Task TriggerEventTest_0()
        {
            using (var t = ConditionTrigger.Push(TestEventTarget))
            {
                await t.Execute(0, "Test");
            }

            Assert.AreEqual(1, EventQueue.Count);

            var e = EventQueue.TryDequeue(out var r);
            Assert.IsTrue(e);
            Assert.AreEqual(TestEventTarget, r.eventTarget);
            Assert.AreEqual(0, (int)r.condition);
            Assert.AreEqual("Test", r.value);
        }
        [Test]
        public async Task Any_0()
        {
            using (var t = ConditionTrigger.Push(TestEventTarget))
            {
                await t.Execute(0, "Test");
                await t.Execute((Model.Condition)1, "Test");
                await t.Execute((Model.Condition)2, "Test");

                Assert.IsTrue(ConditionTrigger.Any(TestEventTarget, 0));
                Assert.IsTrue(ConditionTrigger.Any(TestEventTarget, (Model.Condition)1));
                Assert.IsTrue(ConditionTrigger.Any(TestEventTarget, (Model.Condition)2));
            }
        }
        [Test]
        public async Task Any_1()
        {
            using (var t = ConditionTrigger.Push(TestEventTarget))
            {
                await t.Execute(0, "Test");
                await t.Execute((Model.Condition)1, "Test");
                await t.Execute((Model.Condition)2, "Test");

                Assert.IsTrue(ConditionTrigger.Any(TestEventTarget, 0, "Test"));
                Assert.IsTrue(ConditionTrigger.Any(TestEventTarget, (Model.Condition)1, "Test"));
                Assert.IsTrue(ConditionTrigger.Any(TestEventTarget, (Model.Condition)2, "Test"));
            }
        }
        [Test]
        public async Task Last_0()
        {
            using (var t = ConditionTrigger.Push(TestEventTarget))
            {
                await t.Execute(0, "Test");
                await t.Execute((Model.Condition)1, "Test");
                await t.Execute((Model.Condition)2, "Test");

                Assert.IsTrue(ConditionTrigger.Last(TestEventTarget, 0, "Test"));
                Assert.IsTrue(ConditionTrigger.Last(TestEventTarget, (Model.Condition)1, "Test"));
                Assert.IsTrue(ConditionTrigger.Last(TestEventTarget, (Model.Condition)2, "Test"));
            }
        }
        [Test]
        public async Task Last_1()
        {
            using var t0 = ConditionTrigger.Push(TestEventTarget);
            await t0.Execute(0, "Test");
            await t0.Execute((Model.Condition)1, "Test");
            await t0.Execute((Model.Condition)2, "Test");

            using var t1 = ConditionTrigger.Push(TestEventTarget);
            await t1.Execute((Model.Condition)5, "Test");

            Assert.IsTrue(ConditionTrigger.Last(TestEventTarget, (Model.Condition)5));
            Assert.IsTrue(ConditionTrigger.Last(TestEventTarget, (Model.Condition)5, "Test"));
        }
        [Test]
        public async Task Scope()
        {
            int executed = 0;
            using var scope = ConditionTrigger.Scope(async (target, condition, value) =>
            {
                executed++;
            });
            using var t0    = ConditionTrigger.Push(TestEventTarget);
            await t0.Execute(0, "Test");

            Assert.AreEqual(1, executed);
        }
    }
}