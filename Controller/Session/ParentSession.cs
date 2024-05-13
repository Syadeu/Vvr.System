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
// File created : 2024, 05, 10 15:05

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
    public abstract class ParentSession<TSessionData>
        : ChildSession<TSessionData>, IParentSession, IGameSessionCallback

        where TSessionData : ISessionData
    {
        private readonly List<IChildSession> m_ChildSessions  = new();

        public IReadOnlyList<IChildSession> ChildSessions     => m_ChildSessions;

        protected override UniTask OnReserve()
        {
            m_ChildSessions.Clear();

            return base.OnReserve();
        }
        UniTask IGameSessionCallback.OnSessionClosed(IGameSessionBase child)
        {
            IChildSession session = (IChildSession)child;
            m_ChildSessions.Remove(session);

            return UniTask.CompletedTask;
        }

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
    }
}