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
// File created : 2024, 05, 17 17:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class ActorFactorySession : ChildSession<ActorFactorySession.SessionData>,
        IActorProvider,
        IConnector<IEventViewProvider>,
        IConnector<IStatConditionProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(ActorFactorySession);

        struct CachedActor : IComparable<CachedActor>
        {
            public          uint   hash;
            public readonly IActor actor;

            public CachedActor(IActor t)
            {
                hash  = FNV1a32.Calculate(t.Id);
                actor = t;
            }

            public int CompareTo(CachedActor other)
            {
                return hash.CompareTo(other.hash);
            }
        }

        private IEventViewProvider     m_ViewProvider;
        private IStatConditionProvider m_StatConditionProvider;

        private readonly List<CachedActor> m_ResolvedActors = new();

        protected override UniTask OnReserve()
        {
            for (int i = 0; i < m_ResolvedActors.Count; i++)
            {
                IActor e = m_ResolvedActors[i].actor;

                m_ViewProvider?.Release(e);
                e.Release();
            }
            m_ResolvedActors.Clear();

            return base.OnReserve();
        }

        public IReadOnlyActor Resolve(IActorData data)
        {
            using var timer = DebugTimer.Start();

            // TODO: Should remove this Prototype pattern?
            // I thought making new instance for every call(& initialize as well)
            // would be critical to system.
            // so my intention in very first design was the actor initialized at once when it's resolved.
            // but later i realize this usage adding complexity to system.
            // because all the components are independently works...

            int i = m_ResolvedActors.BinarySearch(new CachedActor() { hash = FNV1a32.Calculate(data.Id) });
            if (0 <= i) return m_ResolvedActors[i].actor;

            Actor.Actor actor = ScriptableObject.CreateInstance<Actor.Actor>();
            actor.Owner = Owner;
            actor.Id    = data.Id;

            m_ResolvedActors.Add(new CachedActor(actor));
            m_ResolvedActors.Sort();
            return actor;
        }

        public IActor Create(Owner owner, IActorData data)
        {
            Assert.IsNotNull(m_StatConditionProvider);

            IReadOnlyActor    template = Resolve(data);
            Assert.IsNotNull(template);

            IActor result = template.CreateInstance();
            Assert.IsNotNull(result);

            result.Initialize(owner, m_StatConditionProvider, data);

            return result;
        }

        void IConnector<IEventViewProvider>.Connect(IEventViewProvider    t) => m_ViewProvider = t;
        void IConnector<IEventViewProvider>.Disconnect(IEventViewProvider t) => m_ViewProvider = null;

        void IConnector<IStatConditionProvider>.Connect(IStatConditionProvider    t) => m_StatConditionProvider = t;
        void IConnector<IStatConditionProvider>.Disconnect(IStatConditionProvider t) => m_StatConditionProvider = null;
    }
}