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
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.Provider;

namespace Vvr.Controller.Session
{
    public abstract class ParentSession<TSessionData>
        : ChildSession<TSessionData>, IParentSession,
            IGameSessionCallback
        where TSessionData : ISessionData
    {
        private readonly List<IChildSession> m_ChildSessions  = new();

        public  IReadOnlyList<IChildSession> ChildSessions => m_ChildSessions;

        protected override async UniTask OnReserve()
        {
            await CloseAllSessions();
        }

        public async UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data)
            where TChildSession : IChildSession
        {
            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type          childType = typeof(TChildSession);
            TChildSession session   = (TChildSession)Activator.CreateInstance(childType);

            if (session is IChildSessionConnector sessionConnector)
            {
                $"[Session: {Type.FullName}] Chain connector to {childType.FullName}".ToLog();
                foreach (var item in ConnectedProviders)
                {
                    var pType = item.Key;
                    sessionConnector.Register(pType, item.Value);
                }
            }
            else $"[Session: {Type.FullName}] No connector for {childType.FullName}".ToLog();

            await session.Initialize(Owner);
            m_ChildSessions.Add(session);

            await OnCreateSession(session);

            await session.Initialize(this, data);
            return session;
        }
        async UniTask IGameSessionCallback.OnSessionClose(IGameSessionBase child)
        {
            IChildSession session = (IChildSession)child;

            await OnSessionClose(session);
            m_ChildSessions.Remove(session);
        }

        public async UniTask CloseAllSessions()
        {
            foreach (var session in m_ChildSessions)
            {
                await session.Reserve();
            }
            m_ChildSessions.Clear();
        }

        protected sealed override void OnProviderRegistered(Type providerType, IProvider provider)
        {
            foreach (var childSession in ChildSessions)
            {
                if (ReferenceEquals(childSession, provider)) continue;
                if (childSession is not IChildSessionConnector c) continue;

                c.Register(providerType, provider);
            }
        }
        protected sealed override void OnProviderUnregistered(Type providerType)
        {
            foreach (var childSession in ChildSessions)
            {
                if (childSession is not IChildSessionConnector c) continue;

                c.Unregister(providerType);
            }
        }

        /// <summary>
        /// Event method when child session has been created
        /// </summary>
        /// <param name="session">The child session that has been created</param>
        /// <returns>A UniTask representing the asynchronous operation</returns>
        [PublicAPI]
        protected virtual UniTask OnCreateSession(IChildSession session) => UniTask.CompletedTask;

        /// <summary>
        /// Event method that is called when a child session is about to be closed and disposed.
        /// </summary>
        /// <param name="session">The child session that is about to be closed</param>
        /// <returns>A UniTask representing the asynchronous operation</returns>
        [PublicAPI]
        protected virtual UniTask OnSessionClose(IChildSession session) => UniTask.CompletedTask;
    }
}