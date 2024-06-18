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

using Assert = NUnit.Framework.Assert;

namespace Vvr.Session.ContentView.Tests
{
    [TestFixture]
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

        private async UniTask RecursiveEvent1Method(TestContentViewEvent e, object ctx)
        {
            Debug.Log($"Executed: {e} with {ctx}");
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
        }
        private async UniTask RecursiveEvent2Method(TestContentViewEvent e, object ctx)
        {
            Debug.Log($"Executed: {e} with {ctx}");
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test2);
        }

        private UniTask RegisterOnMainThread(TestContentViewEvent e, ContentViewEventDelegate<TestContentViewEvent> x)
        {
            return UniTask.Create(
                () =>
                {
                    Debug.Log($"{e} registered");
                    EventHandler.Register(e, x);
                    return UniTask.CompletedTask;
                });
        }
        private UniTask RegisterOnBackground(TestContentViewEvent e, ContentViewEventDelegate<TestContentViewEvent> x)
        {
            return UniTask.RunOnThreadPool(
                () =>
                {
                    Debug.Log($"{e} registered");
                    return EventHandler.Register(e, x);
                });
        }
        private UniTask UnregisterOnMainThread(TestContentViewEvent e, ContentViewEventDelegate<TestContentViewEvent> x)
        {
            return UniTask.Create(
                () =>
                {
                    Debug.Log($"{e} unregistered");
                    EventHandler.Unregister(e, x);

                    return UniTask.CompletedTask;
                });
        }
        private Task UnregisterOnBackground(TestContentViewEvent e, ContentViewEventDelegate<TestContentViewEvent> x)
        {
            return Task.Run(
                () =>
                {
                    Debug.Log($"{e} unregistered");
                    return EventHandler.Unregister(e, x);
                });
        }

        private async Task ExecuteOnMainThread(TestContentViewEvent e, object ctx = null)
        {
            Debug.Log($"{e} execute with ctx");
            await EventHandler.ExecuteAsync(e, ctx);
        }
        private Task ExecuteOnBackground(TestContentViewEvent e, object ctx = null)
        {
            return Task.Run(() =>
            {
                Debug.Log($"{e} execute with ctx");
                return EventHandler.ExecuteAsync(e, ctx);
            });
        }

        [Test]
        public void RegistrationTest_0()
        {
            EventHandler.Register(TestContentViewEvent.Test0, TestMethod);
            EventHandler.Register(TestContentViewEvent.Test1, TestMethod);
            EventHandler.Register(TestContentViewEvent.Test2, TestMethod);
            EventHandler.Register(TestContentViewEvent.Test3, TestMethod);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public void RegistrationTest_1()
        {
            EventHandler.Register(TestContentViewEvent.Test0, TestMethod);
            Assert.Catch<InvalidOperationException>(
                () => EventHandler.Register(TestContentViewEvent.Test0, TestMethod));

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task RegistrationTest_2()
        {
            await UniTask.WhenAll(
                RegisterOnMainThread(TestContentViewEvent.Test0, TestMethod),
                RegisterOnMainThread(TestContentViewEvent.Test1, TestMethod),

                RegisterOnBackground(TestContentViewEvent.Test2, TestMethod),
                RegisterOnBackground(TestContentViewEvent.Test3, TestMethod)
            );

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task ExecuteTest_0()
        {
            await UniTask.WhenAll(
                RegisterOnMainThread(TestContentViewEvent.Test0, TestMethod),
                RegisterOnMainThread(TestContentViewEvent.Test1, TestMethod),

                RegisterOnBackground(TestContentViewEvent.Test2, TestMethod),
                RegisterOnBackground(TestContentViewEvent.Test3, TestMethod)
            );

            await EventHandler.ExecuteAsync(TestContentViewEvent.Test0);
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test2);
            await EventHandler.ExecuteAsync(TestContentViewEvent.Test3);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task ExecuteTest_1()
        {
            await UniTask.WhenAll(
                RegisterOnMainThread(TestContentViewEvent.Test0, TestMethod),
                RegisterOnMainThread(TestContentViewEvent.Test1, TestMethod),

                RegisterOnBackground(TestContentViewEvent.Test2, TestMethod),
                RegisterOnBackground(TestContentViewEvent.Test3, TestMethod)
            );

            await UniTask.WhenAll(
                    EventHandler.ExecuteAsync(TestContentViewEvent.Test0),
                    EventHandler.ExecuteAsync(TestContentViewEvent.Test1),
                    UniTask.RunOnThreadPool(() => EventHandler.ExecuteAsync(TestContentViewEvent.Test2)),
                    UniTask.RunOnThreadPool(() => EventHandler.ExecuteAsync(TestContentViewEvent.Test3))
            );

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task ExecuteTest_2()
        {
            await UniTask.WhenAll(
                RegisterOnBackground(TestContentViewEvent.Test0, RecursiveEvent1Method),
                RegisterOnBackground(TestContentViewEvent.Test1, RecursiveEvent2Method),
                RegisterOnBackground(TestContentViewEvent.Test2, TestMethod)
            );

            await UniTask.RunOnThreadPool(() => EventHandler.ExecuteAsync(TestContentViewEvent.Test0));

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task ExecuteTest_3()
        {
            bool
                e0 = false,
                e1 = false;

            await UniTask.WhenAll(
                RegisterOnBackground(TestContentViewEvent.Test0, RecursiveEvent1Method),
                RegisterOnBackground(TestContentViewEvent.Test0, async (e, x) =>
                {
                    e0 = true;
                }),
                RegisterOnBackground(TestContentViewEvent.Test1, RecursiveEvent2Method),
                RegisterOnBackground(TestContentViewEvent.Test2, TestMethod),
                RegisterOnBackground(TestContentViewEvent.Test2, async (e, x) =>
                {
                    e1 = true;
                })
            );

            await ExecuteOnBackground(TestContentViewEvent.Test0);

            Assert.IsTrue(e0);
            Assert.IsTrue(e1);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }

        [Test]
        public async Task MultiExecuteTest_0()
        {
            bool
                e0 = false,
                e1 = false;

            EventHandler.Register(TestContentViewEvent.Test0, async (e, x) =>
            {
                await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
            });
            EventHandler.Register(TestContentViewEvent.Test1, async (e, x) =>
            {
                e0 = true;
                Debug.Log($"{e} end");
            });
            EventHandler.Register(TestContentViewEvent.Test2, async (e, x) =>
            {
                await EventHandler.ExecuteAsync(TestContentViewEvent.Test3);
            });
            EventHandler.Register(TestContentViewEvent.Test3, async (e, x) =>
            {
                e1 = true;
                Debug.Log($"{e} end");
            });

            await UniTask.WhenAll(
                EventHandler.ExecuteAsync(TestContentViewEvent.Test0),
                EventHandler.ExecuteAsync(TestContentViewEvent.Test2));

            Assert.IsTrue(e0);
            Assert.IsTrue(e1);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task MultiExecuteTest_1()
        {
            bool
                e0 = false,
                e1 = false;

            EventHandler.Register(TestContentViewEvent.Test0, async (e, x) =>
            {
                await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
            });
            EventHandler.Register(TestContentViewEvent.Test1, async (e, x) =>
            {
                e0 = true;
                Debug.Log($"{e} end");
            });
            EventHandler.Register(TestContentViewEvent.Test2, async (e, x) =>
            {
                await EventHandler.ExecuteAsync(TestContentViewEvent.Test3);
            });
            EventHandler.Register(TestContentViewEvent.Test3, async (e, x) =>
            {
                e1 = true;
                Debug.Log($"{e} end");
            });

            await Task.WhenAll(
                ExecuteOnBackground(TestContentViewEvent.Test0),
                ExecuteOnBackground(TestContentViewEvent.Test2)
            );

            Assert.IsTrue(e0);
            Assert.IsTrue(e1);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }

        [Test]
        public async Task MultiExecuteTest_2()
        {
            bool
                e0 = false,
                e1 = false;

            EventHandler.Register(TestContentViewEvent.Test0,
                async (e, x) => { await EventHandler.ExecuteAsync(TestContentViewEvent.Test1); });
            EventHandler.Register(TestContentViewEvent.Test1, async (e, x) =>
            {
                await UniTask.Yield();
                e0 = true;
                Debug.Log($"{e} end");
            });
            EventHandler.Register(TestContentViewEvent.Test2,
                async (e, x) => { await EventHandler.ExecuteAsync(TestContentViewEvent.Test3); });
            EventHandler.Register(TestContentViewEvent.Test3, async (e, x) =>
            {
                await UniTask.Yield();
                e1 = true;
                Debug.Log($"{e} end");
            });

            await UniTask.WhenAll(
                EventHandler.ExecuteAsync(TestContentViewEvent.Test0),
                EventHandler.ExecuteAsync(TestContentViewEvent.Test2));

            Assert.IsTrue(e0);
            Assert.IsTrue(e1);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task MultiExecuteTest_3()
        {
            bool
                e0 = false,
                e1 = false;

            EventHandler.Register(TestContentViewEvent.Test0,
                async (e, x) =>
                {
                    Debug.Log($"{e} in");
                    await UniTask.Yield();
                    await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
                    await UniTask.Yield();
                });
            EventHandler.Register(TestContentViewEvent.Test1, async (e, x) =>
            {
                await UniTask.Yield();
                e0 = true;
                Debug.Log($"{e} end");
            });
            EventHandler.Register(TestContentViewEvent.Test2,
                async (e, x) =>
                {
                    Debug.Log($"{e} in");
                    await EventHandler.ExecuteAsync(TestContentViewEvent.Test3);
                });
            EventHandler.Register(TestContentViewEvent.Test3, async (e, x) =>
            {
                await UniTask.Yield();
                e1 = true;
                Debug.Log($"{e} end");
            });

            await Task.WhenAll(
                ExecuteOnMainThread(TestContentViewEvent.Test0),
                ExecuteOnBackground(TestContentViewEvent.Test2)
                );

            Assert.IsTrue(e0);
            Assert.IsTrue(e1);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }
        [Test]
        public async Task MultiExecuteTest_4()
        {
            bool
                e0 = false,
                e1 = false;

            EventHandler.Register(TestContentViewEvent.Test0,
                async (e, x) =>
                {
                    Debug.Log($"{e} in");
                    await UniTask.Yield();
                    await EventHandler.ExecuteAsync(TestContentViewEvent.Test1);
                    await UniTask.Yield();
                });
            EventHandler.Register(TestContentViewEvent.Test1, async (e, x) =>
            {
                await UniTask.Yield();
                e0 = true;
                Debug.Log($"{e} end");
            });
            EventHandler.Register(TestContentViewEvent.Test2,
                async (e, x) =>
                {
                    Debug.Log($"{e} in");
                    await EventHandler.ExecuteAsync(TestContentViewEvent.Test3);
                });
            EventHandler.Register(TestContentViewEvent.Test3, async (e, x) =>
            {
                await UniTask.Yield();
                e1 = true;
                Debug.Log($"{e} end");
            });

            await Task.WhenAll(
                ExecuteOnMainThread(TestContentViewEvent.Test0)
                ,
                // ExecuteOnMainThread(TestContentViewEvent.Test0),

                // ExecuteOnBackground(TestContentViewEvent.Test2),
                ExecuteOnBackground(TestContentViewEvent.Test2)
                );

            Assert.IsTrue(e0);
            Assert.IsTrue(e1);

            Assert.IsFalse(EventHandler.WriteLocked);
            Assert.IsFalse(EventHandler.ExecutionLocked);
        }

        [Test]
        public async Task InterceptionTest_0()
        {
            bool e0 = false;

            await Task.WhenAll(
                RegisterOnMainThread(TestContentViewEvent.Test0, async (e, x) =>
                {
                    e0 = true;
                }).AsTask(),

                ExecuteOnMainThread(TestContentViewEvent.Test0)
            );

            Assert.IsTrue(e0);
            Assert.IsFalse(EventHandler.ExecutionLocked);
            Assert.IsFalse(EventHandler.WriteLocked);
        }
        [Test]
        public async Task InterceptionTest_1()
        {
            bool e0 = false;

            await Task.WhenAll(
                RegisterOnBackground(TestContentViewEvent.Test0, async (e, x) =>
                {
                    e0 = true;
                }).AsTask(),

                ExecuteOnMainThread(TestContentViewEvent.Test0)
            );

            await ExecuteOnMainThread(TestContentViewEvent.Test0);

            Assert.IsTrue(e0);
            Assert.IsFalse(EventHandler.ExecutionLocked);
            Assert.IsFalse(EventHandler.WriteLocked);
        }
        [Test]
        public async Task InterceptionTest_2()
        {
            bool e0 = false;

            Assert.IsFalse(EventHandler.CancellationToken.IsCancellationRequested);
            await Task.WhenAll(
                RegisterOnBackground(TestContentViewEvent.Test0, async (e, x) =>
                {
                    e0 = true;
                }).AsTask(),

                ExecuteOnBackground(TestContentViewEvent.Test0)
            );

            Assert.IsFalse(EventHandler.ExecutionLocked);
            Assert.IsFalse(EventHandler.WriteLocked);
        }
        [Test]
        public async Task InterceptionTest_3()
        {
            bool e0 = false;

            Assert.IsFalse(EventHandler.CancellationToken.IsCancellationRequested);
            await Task.WhenAll(
                RegisterOnBackground(TestContentViewEvent.Test0, async (e, x) =>
                {
                    e0 = true;
                }).AsTask(),

                ExecuteOnBackground(TestContentViewEvent.Test0)
            );

            Assert.IsFalse(EventHandler.ExecutionLocked);
            Assert.IsFalse(EventHandler.WriteLocked);

            await ExecuteOnBackground(TestContentViewEvent.Test0);

            Assert.IsTrue(e0);
        }
    }
}