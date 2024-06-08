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
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents an abstract parent session that can create and manage child sessions.
    /// </summary>
    /// <typeparam name="TSessionData">The type of session data stored in the parent session.</typeparam>
    public abstract class ParentSession<TSessionData>
        : ChildSession<TSessionData>, IParentSession,
            IGameSessionCallback
        where TSessionData : ISessionData
    {
        private class SessionCreateContext
        {
            public readonly ParentSession<TSessionData> parentSession;

            public readonly Type          sessionType;
            public readonly IChildSession instance;
            public readonly ISessionData  data;

            public SessionCreateContext(
                ParentSession<TSessionData> t,
                IChildSession ta,
                Type taa, ISessionData taaa)
            {
                parentSession = t;
                instance      = ta;
                sessionType   = taa;
                data          = taaa;
            }
        }

        private readonly List<IChildSession> m_ChildSessions  = new();

        private SpinLock m_CreateSessionLock;

        public  IReadOnlyList<IChildSession> ChildSessions => m_ChildSessions;

        protected override async UniTask OnReserve()
        {
            await CloseAllSessions();
        }

        public async UniTask<TChildSession> CreateSessionOnBackground<TChildSession>(ISessionData data)
            where TChildSession : IChildSession, new()
        {
            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type childType = typeof(TChildSession);
            var  ins       = new TChildSession();
            m_ChildSessions.Add(ins);

            var  ctx       = new SessionCreateContext(this, ins, childType, data);

            await UniTask.RunOnThreadPool(CreateSession, ctx, cancellationToken: ReserveToken);
            return ins;
        }
        public async UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data)
            where TChildSession : IChildSession, new()
        {
            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type childType = typeof(TChildSession);
            var  ins       = new TChildSession();
            m_ChildSessions.Add(ins);
            var  ctx       = new SessionCreateContext(this, ins, childType, data);

            await CreateSession(ctx);

            return ins;
        }

        private static async UniTask CreateSession([NotNull] object state)
        {
            SessionCreateContext ctx     = (SessionCreateContext)state;
            IChildSession        session = ctx.instance;

            await ctx.parentSession.OnCreateSession(session);

            if (session is IDependencyContainer sessionConnector)
            {
                // $"[Session: {ctx.parentSession.Type.FullName}] Chain connector to {ctx.sessionType.FullName}".ToLog();
                using var debugTimer = DebugTimer.Start();
                foreach (var item in ctx.parentSession.ConnectedProviders)
                {
                    var pType = item.Key;
                    sessionConnector.Register(pType, item.Value);
                }
            }
            // else $"[Session: {Type.FullName}] No connector for {childType.FullName}".ToLog();

            await session.Initialize(ctx.parentSession.Owner, ctx.parentSession, ctx.data);
            $"[Session: {ctx.parentSession.Type.FullName}] created {ctx.sessionType.FullName}".ToLog();
        }

        async UniTask IGameSessionCallback.OnSessionClose(IGameSessionBase child)
        {
            IChildSession session = (IChildSession)child;

            Type childType = session.Type;

            await OnSessionClose(session);

            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);

                m_ChildSessions.Remove(session);
            }
            finally
            {
                if (lt) m_CreateSessionLock.Exit();
            }

            $"[Session: {Type.FullName}] closed {childType.FullName}".ToLog();
        }

        public async UniTask CloseAllSessions()
        {
            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);

                for (var i = m_ChildSessions.Count - 1; i >= 0; i--)
                {
                    var session = m_ChildSessions[i];
                    await session.Reserve();
                }

                m_ChildSessions.Clear();
            }
            finally
            {
                if (lt) m_CreateSessionLock.Exit();
            }
        }
        public async UniTask<IChildSession> WaitUntilSessionAvailableAsync(Type sessionType, float timeout)
        {
            Timer timer = Timer.Start();

            IChildSession found;
            while ((found = GetSession(sessionType)) == null)
            {
                await UniTask.Yield();

                if (timeout > 0 && timer.IsExceeded(timeout))
                    throw new TimeoutException();
            }

            return found;
        }
        public async UniTask<TChildSession> WaitUntilSessionAvailableAsync<TChildSession>(float timeout)
            where TChildSession : class, IChildSession
        {
            IChildSession found = await WaitUntilSessionAvailableAsync(typeof(TChildSession), timeout);
            return found as TChildSession;
        }
        public IChildSession GetSession(Type sessionType)
        {
            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);

                foreach (var session in m_ChildSessions)
                {
                    IChildSession found;
                    if (session is IParentSession parentSession &&
                        (found = parentSession.GetSession(sessionType)) != null)
                    {
                        return found;
                    }

                    if (VvrTypeHelper.InheritsFrom(session.Type, sessionType))
                    {
                        return session;
                    }
                }
            }
            finally
            {
                if (lt) m_CreateSessionLock.Exit();
            }

            return null;
        }
        public TChildSession GetSession<TChildSession>() where TChildSession : class, IChildSession
        {
            return GetSession(typeof(TChildSession)) as TChildSession;
        }

        protected sealed override void OnProviderRegistered(Type providerType, IProvider provider)
        {
            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);

                foreach (var childSession in ChildSessions)
                {
                    if (ReferenceEquals(childSession, provider)) continue;
                    if (childSession is not IDependencyContainer c) continue;

                    c.Register(providerType, provider);
                }
            }
            finally
            {
                if (lt) m_CreateSessionLock.Exit();
            }
        }
        protected sealed override void OnProviderUnregistered(Type providerType)
        {
            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);

                foreach (var childSession in ChildSessions)
                {
                    if (childSession is not IDependencyContainer c) continue;

                    c.Unregister(providerType);
                }
            }
            finally
            {
                if (lt) m_CreateSessionLock.Exit();
            }
        }

        /// <summary>
        /// Event method when child session has been created
        /// </summary>
        /// <remarks>
        /// This method executes very first before child session's Initialize method, and injection.
        /// </remarks>
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