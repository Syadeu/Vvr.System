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
// File created : 2024, 06, 23 03:06

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session;
using Vvr.Session.Provider;

namespace Vvr.TestClass
{
    [UsedImplicitly]
    public sealed class TestActorFactorySession : ChildSession<TestActorFactorySession.SessionData>,
        IActorProvider,
        IConnector<IStatConditionProvider>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(TestActorFactorySession);

        private IStatConditionProvider m_StatConditionProvider;

        public IReadOnlyActor Resolve(IActorData data)
        {
            TestActor actor = new TestActor(Owner, data.Id, null);

            return actor;
        }

        public IActor Create(Owner owner, IActorData data)
        {
            TestActor actor = new TestActor(Owner, data.Id, null);
            actor.Initialize(owner, m_StatConditionProvider, data);

            return actor;
        }

        void IConnector<IStatConditionProvider>.Connect(IStatConditionProvider    t) => m_StatConditionProvider = t;
        void IConnector<IStatConditionProvider>.Disconnect(IStatConditionProvider t) => m_StatConditionProvider = null;
    }
}