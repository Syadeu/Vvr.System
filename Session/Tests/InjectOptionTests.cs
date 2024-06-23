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
// File created : 2024, 06, 23 11:06

#endregion

using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Vvr.Model;
using Vvr.Provider;
using Vvr.TestClass;

namespace Vvr.Session.Tests
{
    [TestFixture]
    public sealed class InjectOptionTests : SessionTest<TestRootSession>
    {
        [InjectOptions(Cache = true)]
        class TestSession : HeavyDITestSession
        {

        }

        [InjectOptions(Cache = true)]
        class TestDIClass :
            IConnector<ITestLocalProvider>,
            IConnector<ITestLocalProvider01>,
            IConnector<ITestLocalProvider02>,
            IConnector<ITestLocalProvider03>,
            IConnector<ITestLocalProvider04>
        {
            public ITestLocalProvider Provider00 { get; private set; }
            public ITestLocalProvider01 Provider01 { get; private set; }
            public ITestLocalProvider02 Provider02 { get; private set; }
            public ITestLocalProvider03 Provider03 { get; private set; }
            public ITestLocalProvider04 Provider04 { get; private set; }

            void IConnector<ITestLocalProvider>.Connect(ITestLocalProvider    t) => Provider00 = t;
            void IConnector<ITestLocalProvider>.Disconnect(ITestLocalProvider t) => Provider00 = null;

            public void Connect(ITestLocalProvider01    t) => Provider01 = t;
            public void Disconnect(ITestLocalProvider01 t) => Provider01 = null;

            public void Connect(ITestLocalProvider02 t) => Provider02 = t;
            public void Disconnect(ITestLocalProvider02 t) => Provider02 = null;

            public void Connect(ITestLocalProvider03    t) => Provider03 = t;
            public void Disconnect(ITestLocalProvider03 t) => Provider03 = null;

            public void Connect(ITestLocalProvider04    t) => Provider04 = t;
            public void Disconnect(ITestLocalProvider04 t) => Provider04 = null;
        }

        public override async Task SetUp()
        {
            await base.SetUp();

            Root.Register<ITestLocalProvider>(new TestLocalProvider());
            Root.Register<ITestLocalProvider01>(new TestLocalProvider01());
            Root.Register<ITestLocalProvider02>(new TestLocalProvider02());
            Root.Register<ITestLocalProvider03>(new TestLocalProvider03());
            Root.Register<ITestLocalProvider04>(new TestLocalProvider04());
        }
        public override Task TearDown()
        {
            return base.TearDown();
        }

        [Test]
        public async Task SessionTest_0()
        {
            var t0 = await Root.CreateSession<TestSession>(default);

            Assert.NotNull(t0.Provider00);
            Assert.NotNull(t0.Provider01);
            Assert.NotNull(t0.Provider02);
            Assert.NotNull(t0.Provider03);
            Assert.NotNull(t0.Provider04);
        }
        [Test]
        public void CIL_Test_0()
        {
            var t0 = new TestDIClass();
            Root.Inject(t0);

            Assert.NotNull(t0.Provider00);
            Assert.NotNull(t0.Provider01);
            Assert.NotNull(t0.Provider02);
            Assert.NotNull(t0.Provider03);
            Assert.NotNull(t0.Provider04);
        }
        [Test]
        public void CIL_Test_1()
        {
            var t0 = new TestDIClass();
            Root.Inject(t0);
            Root.Detach(t0);

            Assert.IsNull(t0.Provider00);
            Assert.IsNull(t0.Provider01);
            Assert.IsNull(t0.Provider02);
            Assert.IsNull(t0.Provider03);
            Assert.IsNull(t0.Provider04);
        }
    }
}