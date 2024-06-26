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
// File created : 2024, 06, 26 15:06

#endregion

using NUnit.Framework;
using Vvr.Controller.Stat;
using Vvr.Provider;
using Vvr.TestClass;

namespace Vvr.Controller.Tests
{
    [TestFixture]
    public sealed class StatValueStackTests
    {
        private TestActor      m_Actor;
        private StatValueStack m_Value;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Actor = TestActor.Create(Owner.Issue);
        }
        [SetUp]
        public void SetUp()
        {
            m_Value = new StatValueStack(m_Actor, null);
        }
        [TearDown]
        public void TearDown()
        {
            m_Value.Dispose();
        }

        
    }
}