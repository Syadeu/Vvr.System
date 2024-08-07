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
// File created : 2024, 06, 09 21:06

#endregion

using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vvr.Provider;
using Vvr.TestClass;
using Assert = NUnit.Framework.Assert;

namespace Vvr.Session.Tests
{
    public class DISessionTests : SessionTest<TestRootSession>
    {
        [Test]
        public async Task NoInjectionTest()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);

            Assert.IsNull(t0.Provider);
        }

        [Test]
        public async Task InjectionTest_0()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);

            t0.Register(new TestLocalProvider());

            Assert.IsNotNull(t0.Provider);
        }
        [Test]
        public async Task InjectionTest_1()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);

            Root.Register(new TestLocalProvider());

            Assert.IsNotNull(t0.Provider);
        }
        [Test]
        public async Task InjectionTest_2()
        {
            Root.Register(new TestLocalProvider());

            var t0 = await Root.CreateSession<DITestSession>(null);
            Assert.IsNotNull(t0.Provider);
        }

        [Test]
        public async Task DetachTest_0()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);
            t0.Register(new TestLocalProvider());
            t0.Unregister<TestLocalProvider>();

            Assert.IsNull(t0.Provider);
        }
        [Test]
        public async Task DetachTest_1()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);
            Root.Register(new TestLocalProvider());
            Root.Unregister<TestLocalProvider>();

            Assert.IsNull(t0.Provider);
        }
        [Test]
        public async Task DetachTest_2()
        {
            Root.Register(new TestLocalProvider());
            var t0 = await Root.CreateSession<DITestSession>(null);
            Root.Unregister<TestLocalProvider>();

            Assert.IsNull(t0.Provider);
        }

        [Test]
        public async Task ConnectionTest_0()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);
            var t1 = await Root.CreateSession<DITestSession>(null);

            t1.Register<ITestLocalProvider>(new TestLocalProvider());
            t1.Connect(t0);

            Assert.NotNull(t0.Provider);

            t1.Disconnect(t0);

            Assert.IsNull(t0.Provider);
        }
        [Test]
        public async Task ConnectionTest_1()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);
            var t1 = await t0.CreateSession<DITestSession>(null);
            t1.Register<ITestLocalProvider>(new TestLocalProvider());
            t1.Connect(t0);

            Assert.NotNull(t0.Provider);
            await t1.Reserve();

            Assert.IsNull(t0.Provider);
        }
        [Test]
        public async Task ConnectionTest_2()
        {
            ITestLocalProvider
                p0 = new TestLocalProvider();

            var t0 = await Root.CreateSession<DITestSession>(null);
            var t1 = await t0.CreateSession<DITestSession>(null);
            var t2 = await t1.CreateSession<DITestSession>(null);

            t0.Register(p0);

            Assert.NotNull(t0.Provider);
            Assert.NotNull(t1.Provider);
            Assert.NotNull(t2.Provider);
        }
        [Test]
        public async Task ConnectionTest_3()
        {
            ITestLocalProvider
                p0 = new TestLocalProvider(),
                p1 = new TestLocalProvider();

            var t0 = await Root.CreateSession<DITestSession>(null);
            var t1 = await t0.CreateSession<DITestSession>(null);
            var t2 = await t1.CreateSession<DITestSession>(null);

            t0.Register(p0);
            t1.Register(p1);

            Assert.AreSame(p0, t0.Provider);
            Assert.AreSame(p1, t1.Provider);
            Assert.AreSame(p1, t2.Provider);
        }
        [Test]
        public async Task ConnectionTest_4()
        {
            ITestLocalProvider
                p0 = new TestLocalProvider(),
                p1 = new TestLocalProvider();

            var t0 = await Root.CreateSession<DITestSession>(null);
            var t1 = await t0.CreateSession<DITestSession>(null);
            var t2 = await t1.CreateSession<DITestSession>(null);

            t0.Register(p0);
            t1.Register(p1);
            t1.Unregister<ITestLocalProvider>();

            Assert.AreSame(p0, t0.Provider);
            Assert.AreSame(p0, t1.Provider);
            Assert.AreSame(p0, t2.Provider);
        }
        [Test]
        public async Task ConnectionTest_5()
        {
            ITestLocalProvider
                p0 = new TestLocalProvider();

            var t0 = await Root.CreateSession<DITestSession>(null);

            var lateTask = UniTask.WhenAll(
                t0.CreateSessionOnBackground<DITestSession>(null),
                t0.CreateSessionOnBackground<DITestSession>(null)
            );

            t0.Register(p0);

            var results = await lateTask;

            Assert.AreSame(p0, t0.Provider);
            Assert.AreSame(p0, results.Item1.Provider);
            Assert.AreSame(p0, results.Item2.Provider);
        }

        [Test]
        public async Task HeavyInjectionTest()
        {
            TestLocalProviderBase
                p0 = new TestLocalProvider(),
                p1 = new TestLocalProvider01(),
                p2 = new TestLocalProvider02(),
                p3 = new TestLocalProvider03(),
                p4 = new TestLocalProvider04();

            Root.Register(typeof(ITestLocalProvider), p0);
            Root.Register(typeof(ITestLocalProvider01), p1);
            Root.Register(typeof(ITestLocalProvider02), p2);
            Root.Register(typeof(ITestLocalProvider03), p3);
            Root.Register(typeof(ITestLocalProvider04), p4);

            var t0 = await Root.CreateSession<HeavyDITestSession>(null);

            Assert.IsNotNull(t0.Provider00);
            Assert.IsNotNull(t0.Provider01);
            Assert.IsNotNull(t0.Provider02);
            Assert.IsNotNull(t0.Provider03);
            Assert.IsNotNull(t0.Provider04);
        }
    }
}