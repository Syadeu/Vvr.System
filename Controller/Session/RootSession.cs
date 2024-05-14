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
// File created : 2024, 05, 10 12:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Session
{
    public abstract class RootSession : IParentSession, IGameSessionCallback, IDisposable
    {
        private readonly List<IChildSession> m_ChildSessions = new();
        private          ConditionResolver   m_ConditionResolver;

        public          Owner  Owner       { get; private set; }
        public abstract string DisplayName { get; }

        public IReadOnlyList<IChildSession> ChildSessions     => m_ChildSessions;
        public IReadOnlyConditionResolver   ConditionResolver => m_ConditionResolver;

        public bool Disposed { get; private set; }

        async UniTask IGameSessionBase.Initialize(Owner owner)
        {
            Disposed = false;

            Owner               = owner;
            m_ConditionResolver = Condition.ConditionResolver.Create(this);
            Connect(m_ConditionResolver);

            await OnInitialize();
        }
        async UniTask IGameSessionBase.Reserve()
        {
            await OnReserve();

            m_ConditionResolver.Dispose();

            m_ConditionResolver = null;

            Disposed = true;
        }

        public void Dispose()
        {
            for (int i = m_ChildSessions.Count - 1; i >= 0; i--)
            {
                var e = m_ChildSessions[i];
                e.Reserve().GetAwaiter().GetResult();
            }

            ((IGameSessionBase)this).Reserve().GetAwaiter().GetResult();

            // Assert.AreEqual(GameWorld.s_WorldSession, this);
            // GameWorld.s_WorldSession = null;
        }

        protected virtual UniTask OnInitialize() => UniTask.CompletedTask;
        protected virtual UniTask OnReserve()    => UniTask.CompletedTask;

        [MustUseReturnValue]
        public async UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data)
            where TChildSession : IChildSession
        {
            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            TChildSession session = (TChildSession)Activator.CreateInstance(typeof(TChildSession));
            await session.Initialize(Owner);

            m_ChildSessions.Add(session);

            await OnCreateSession(session);

            await session.Initialize(this, data);

            return session;
        }
        protected virtual UniTask OnCreateSession(IChildSession session) => UniTask.CompletedTask;
        protected virtual UniTask OnSessionClosed(IChildSession session) => UniTask.CompletedTask;

        async UniTask IGameSessionCallback.OnSessionClosed(IGameSessionBase child)
        {
            IChildSession session = (IChildSession)child;

            await OnSessionClosed(session);

            m_ChildSessions.Remove(session);
        }

        protected virtual void Connect(ConditionResolver conditionResolver)
        {
        }
    }
}