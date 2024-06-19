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

using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Tests
{
    [TestFixture]
    public sealed class TypedContentViewEventHandlerTests
    {
        private TypedContentViewEventHandler<TestContentViewEvent> EventHandler   { get; set; }

        private int             ExecutionCount          { get; set; }
        private HashSet<string> ExecutedContextTypeName { get; } = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            EventHandler = new();
        }
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EventHandler.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            Assert.IsFalse(EventHandler.WriteLocked);
            EventHandler.Clear();
        }

        struct TestContext00
        {
            public int    testInt;
            public float  testFloat;
            public string testString;
        }
        struct TestContext01
        {
            public int    testInt;
            public float  testFloat;
            public string testString;
        }

        private async UniTask TestContextEventHandler(TestContentViewEvent e, TestContext00 ctx)
        {
            ExecutionCount++;
            ExecutedContextTypeName.Add(nameof(TestContext00));

            "executed".ToLog();
        }

        [Test]
        public async Task RegisterTest_0()
        {
            EventHandler
                .Register<TestContext00>(TestContentViewEvent.Test0, TestContextEventHandler);

            await EventHandler.ExecuteAsync(TestContentViewEvent.Test0, new TestContext00());

            Assert.AreEqual(1, ExecutionCount);
        }
    }
}