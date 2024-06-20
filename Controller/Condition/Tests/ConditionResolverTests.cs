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
// File created : 2024, 06, 20 21:06

#endregion

using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vvr.TestClass;

namespace Vvr.Controller.Condition.Tests
{
    [TestFixture]
    public sealed class ConditionResolverTests
    {
        private TestConditionTarget   TestEventTarget   { get; } = new(nameof(ConditionResolverTests));
        private ConditionResolver ConditionResolver { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ConditionResolver                 = ConditionResolver.Create(TestEventTarget, null);
            TestEventTarget.ConditionResolver = ConditionResolver;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ConditionResolver.Dispose();

            Assert.IsTrue(ConditionResolver.Disposed);
        }

        [SetUp]
        public void SetUp()
        {
            if (ConditionResolver.Disposed)
                ConditionResolver = ConditionResolver.Create(TestEventTarget, null);
            TestEventTarget.ConditionResolver = ConditionResolver;
        }
        [TearDown]
        public void TearDown()
        {
            ConditionResolver.Clear();
        }

        [Test]
        public void ResolveTest_0()
        {
            ConditionResolver[(Model.Condition)1] = _ => true;
            ConditionResolver[(Model.Condition)2] = _ => false;
            ConditionResolver[(Model.Condition)3] = _ => true;

            Assert.IsTrue(ConditionResolver[(Model.Condition)1](null));
            Assert.IsFalse(ConditionResolver[(Model.Condition)2](null));
            Assert.IsTrue(ConditionResolver[(Model.Condition)3](null));
        }
        [Test]
        public void ResolveTest_1()
        {
            ConditionResolver[(Model.Condition)1] = value => value == "Test";
            ConditionResolver[(Model.Condition)2] = value => value != "Test";
            ConditionResolver[(Model.Condition)3] = value => value == "Test";

            Assert.IsFalse(ConditionResolver[(Model.Condition)1](null));
            Assert.IsFalse(ConditionResolver[(Model.Condition)2]("Test"));
            Assert.IsFalse(ConditionResolver[(Model.Condition)3](null));
        }

        [Test]
        public async Task ResolveTest_2()
        {
            await UniTask.WhenAll(
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)1] = _ => true),
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)2] = _ => false),
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)3] = _ => true)
            );

            Assert.IsTrue(ConditionResolver[(Model.Condition)1](null));
            Assert.IsFalse(ConditionResolver[(Model.Condition)2](null));
            Assert.IsTrue(ConditionResolver[(Model.Condition)3](null));
        }
        [Test]
        public async Task ResolveTest_3()
        {
            await UniTask.WhenAll(
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)1] = _ => true),
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)2] = _ => false),
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)3] = _ => true)
            );

            Assert.Catch<InvalidOperationException>(
                () => ConditionResolver[(Model.Condition)2] = value => { return true; });

            Assert.IsTrue(ConditionResolver[(Model.Condition)1](null));
            Assert.IsFalse(ConditionResolver[(Model.Condition)2](null));
            Assert.IsTrue(ConditionResolver[(Model.Condition)3](null));
        }
        [Test]
        public async Task ResolveTest_4()
        {
            await UniTask.WhenAll(
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)1] = _ => true),
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)2] = _ => false),
                UniTask.RunOnThreadPool(() => ConditionResolver[(Model.Condition)3] = _ => true)
            );

            ConditionResolver[(Model.Condition)2] = null;
            ConditionResolver[(Model.Condition)2] = value => { return true; };

            Assert.IsTrue(ConditionResolver[(Model.Condition)1](null));
            Assert.IsTrue(ConditionResolver[(Model.Condition)2](null));
            Assert.IsTrue(ConditionResolver[(Model.Condition)3](null));
        }

        [Test]
        public async Task SubscribeTest_0()
        {
            ConditionResolver[(Model.Condition)1] = _ => true;
            ConditionResolver[(Model.Condition)2] = _ => true;
            ConditionResolver[(Model.Condition)3] = _ => true;

            int executeCount = 0;

            using var ob = ConditionResolver.CreateObserver();
            ob[(Model.Condition)1] = async (_, _) => executeCount++;
            ob[(Model.Condition)2] = async (_, _) => executeCount++;
            ob[(Model.Condition)3] = async (_, _) => executeCount++;

            using (var trigger = ConditionTrigger.Push(TestEventTarget))
            {
                await trigger.Execute((Model.Condition)1, null);
                await trigger.Execute((Model.Condition)2, null);
                await trigger.Execute((Model.Condition)3, null);
            }

            Assert.AreEqual(3, executeCount);
        }
    }
}