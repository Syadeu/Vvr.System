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
// File created : 2024, 06, 22 20:06

#endregion

using System.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Session.Provider;
using Vvr.TestClass;

namespace Vvr.Session.Tests
{
    [PublicAPI]
    public abstract class ActorSessionTestBase : SessionWithDataTest<TestRootSession>
    {
        private int m_Index;

        public IActorProvider ActorProvider { get; private set; }

        public override async Task OneTimeSetUp()
        {
            await base.OneTimeSetUp();

            ActorProvider = await Root.CreateSession<TestActorFactorySession>(default);
        }

        public override Task TearDown()
        {
            m_Index = 0;

            return base.TearDown();
        }

        public TestActorData CreateActorData(
            string               id,
            ActorSheet.ActorType actorType, int grade, int population)
        {
            var data = new TestActorData(
                id, m_Index++, actorType, grade, population, TestStatValues.CreateRandom());

            return data;
        }
    }
}