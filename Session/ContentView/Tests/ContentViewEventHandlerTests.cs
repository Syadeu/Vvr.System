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
// File created : 2024, 06, 10 00:06

#endregion

using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Tests
{
    public sealed class ContentViewEventHandlerTests
    {
        private ContentViewEventHandler<TestContentViewEvent> EventHandler { get; set; }

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
            EventHandler.Clear();
        }

        private UniTask TestMethod(TestContentViewEvent e, object ctx)
        {
            Debug.Log($"Executed: {e} with {ctx}");
            return UniTask.CompletedTask;
        }

        [Test]
        public void RegistrationTest_0()
        {
            EventHandler.Register(TestContentViewEvent.Test0, TestMethod);
            EventHandler.Register(TestContentViewEvent.Test1, TestMethod);
            EventHandler.Register(TestContentViewEvent.Test2, TestMethod);
            EventHandler.Register(TestContentViewEvent.Test3, TestMethod);
        }
        [Test]
        public void RegistrationTest_1()
        {
            EventHandler.Register(TestContentViewEvent.Test0, TestMethod);
            Assert.Catch<InvalidOperationException>(
                () => EventHandler.Register(TestContentViewEvent.Test0, TestMethod));
        }
        [Test]
        public async Task RegistrationTest_2()
        {
            await UniTask.WhenAll(
                UniTask.Create(async () => EventHandler.Register(TestContentViewEvent.Test0, TestMethod)),
                UniTask.Create(async () => EventHandler.Register(TestContentViewEvent.Test1, TestMethod)),
                UniTask.RunOnThreadPool(async () => EventHandler.Register(TestContentViewEvent.Test2, TestMethod)),
                UniTask.RunOnThreadPool(async () => EventHandler.Register(TestContentViewEvent.Test3, TestMethod))
            );
        }
        [Test]
        public async Task ExecuteTest_0()
        {
            await UniTask.WhenAll(
                UniTask.Create(async () => EventHandler.Register(TestContentViewEvent.Test0, TestMethod)),
                UniTask.Create(async () => EventHandler.Register(TestContentViewEvent.Test1, TestMethod)),
                UniTask.RunOnThreadPool(async () => EventHandler.Register(TestContentViewEvent.Test2, TestMethod)),
                UniTask.RunOnThreadPool(async () => EventHandler.Register(TestContentViewEvent.Test3, TestMethod))
            );

            await EventHandler.ExecuteAsync(TestContentViewEvent.Test0);
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test2);
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test3);
        }
    }
}