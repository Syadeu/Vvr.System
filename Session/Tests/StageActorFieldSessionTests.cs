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
// File created : 2024, 06, 22 17:06

#endregion

using System.Threading.Tasks;
using NUnit.Framework;
using Vvr.Session.Provider;
using Vvr.TestClass;

namespace Vvr.Session.Tests
{
    [TestFixture]
    public sealed class StageActorFieldSessionTests : SessionWithDataTest<TestRootSession>
    {
        private IStageActorProvider    FactorySession { get; set; }
        private StageActorFieldSession FieldSession   { get; set; }

        public override async Task OneTimeSetUp()
        {
            await base.OneTimeSetUp();

            FactorySession = await Root.CreateSession<StageActorFactorySession>(default);
            FieldSession   = await Root.CreateSession<StageActorFieldSession>(default);
        }

        public override Task TearDown()
        {
            FactorySession.Clear();
            FieldSession.Clear();

            return base.TearDown();
        }

        // public TestActor CreateActor(string displayName)
        // {
        //     TestActor actor = new TestActor(Root.Owner, displayName, )
        // }
    }
}