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

using System;
using System.Linq;
using NUnit.Framework;
using Vvr.TestClass;

namespace Vvr.Session.Tests
{
    public class RootSessionTests : SessionTest<TestRootSession>
    {
        [Test]
        public async void CreateAndReserveTest()
        {
            var ins = await Root.CreateSession<TestChildSession>(null);
            Assert.IsTrue(Root.ChildSessions.Contains(ins));

            await ins.Reserve();

            Assert.IsTrue(ins.Disposed);
            Assert.IsFalse(Root.ChildSessions.Contains(ins));
        }

        [Test]
        public async void CreateMultipleSameSessionTest()
        {
            IChildSession
                t0 = await Root.CreateSession<TestChildSession>(null),
                t1 = await Root.CreateSession<TestChildSession>(null),
                t2 = await Root.CreateSession<TestChildSession>(null);

            Assert.IsTrue(Root.ChildSessions.Contains(t0));
            Assert.IsTrue(Root.ChildSessions.Contains(t1));
            Assert.IsTrue(Root.ChildSessions.Contains(t2));

            await t0.Reserve();
            await t1.Reserve();
            await t2.Reserve();

            Assert.IsFalse(Root.ChildSessions.Contains(t0));
            Assert.IsFalse(Root.ChildSessions.Contains(t1));
            Assert.IsFalse(Root.ChildSessions.Contains(t2));
        }

        [Test]
        public async void BuildHierarchyTest_0()
        {
            var t0 = await Root.CreateSession<TestParentSession>(null);
            var t1 = await t0.CreateSession<TestChildSession>(null);

            Assert.IsTrue(t0.ChildSessions.Contains(t1));

            await t0.Reserve();

            Assert.IsTrue(t1.Disposed);
            Assert.IsTrue(t0.Disposed);

            Assert.Catch<ObjectDisposedException>(() => _ = t0.ChildSessions);
        }
        [Test]
        public async void BuildHierarchyTest_1()
        {
            var t0 = await Root.CreateSession<TestParentSession>(null);
            var t1 = await t0.CreateSession<TestParentSession>(null);
            var t2 = await t1.CreateSession<TestChildSession>(null);

            Assert.IsTrue(t0.ChildSessions.Contains(t1));
            Assert.IsTrue(t1.ChildSessions.Contains(t2));

            await t1.Reserve();

            Assert.IsTrue(t2.Disposed);
            Assert.IsTrue(t1.Disposed);

            await t0.Reserve();
            Assert.IsTrue(t0.Disposed);
        }
        [Test]
        public async void BuildHierarchyTest_2()
        {
            var t0 = await Root.CreateSession<TestParentSession>(null);
            var t1 = await t0.CreateSession<TestParentSession>(null);
            var t2 = await t1.CreateSession<TestChildSession>(null);

            await t0.CloseAllSessions();

            Assert.IsTrue(t2.Disposed);
            Assert.IsTrue(t1.Disposed);
            Assert.IsFalse(t0.Disposed);
        }
    }
}