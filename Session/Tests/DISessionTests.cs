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

using NUnit.Framework;
using Vvr.TestClass;
using Assert = NUnit.Framework.Assert;

namespace Vvr.Session.Tests
{
    public class DISessionTests : SessionTest<TestRootSession>
    {
        [Test]
        public async void NoInjectionTest()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);

            Assert.IsNull(t0.Provider);
        }

        [Test]
        public async void InjectionTest_0()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);

            t0.Register(new TestLocalProvider());

            Assert.IsNotNull(t0.Provider);
        }
        [Test]
        public async void InjectionTest_1()
        {
            var t0 = await Root.CreateSession<DITestSession>(null);

            Root.Register(new TestLocalProvider());

            Assert.IsNotNull(t0.Provider);
        }
        [Test]
        public async void InjectionTest_2()
        {
            Root.Register(new TestLocalProvider());

            var t0 = await Root.CreateSession<DITestSession>(null);
            Assert.IsNotNull(t0.Provider);
        }
    }
}