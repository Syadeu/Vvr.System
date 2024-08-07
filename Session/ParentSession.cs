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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;
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
        private class ProviderRegistrationContext
        {
            public IDependencyContainer container;
            public Type                 providerType;
            public IProvider            provider;
        }
#if UNITY_EDITOR
        class SessionCreationBlock : IDisposable
        {
            public HashSet<Type> RequestedProviderTypes   { get; }

            public SessionCreationBlock(Type targetType)
            {
                var pAtt = targetType.GetCustomAttribute<ProviderSessionAttribute>();

                if (pAtt is not null)
                    RequestedProviderTypes = new(pAtt.ProviderTypes ?? Array.Empty<Type>());
                else
                    RequestedProviderTypes = new();
            }

            public void Dispose()
            {
                RequestedProviderTypes.Clear();
            }
        }

        private readonly AsyncLocal<SessionCreationBlock> m_SessionCreationBlock = new();
#endif

        private readonly List<IChildSession> m_ChildSessions  = new();

        private SpinLock m_CreateSessionLock;

        public  IReadOnlyList<IChildSession> ChildSessions
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(Type.Name);
                return m_ChildSessions;
            }
        }

        protected override async UniTask OnReserve()
        {
            await CloseAllSessions();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public async UniTask<TChildSession> CreateSessionOnBackground<TChildSession>(ISessionData data)
            where TChildSession : IChildSession, new()
        {
            if (Disposed)
                throw new ObjectDisposedException(Type.Name);

            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type childType = typeof(TChildSession);
            var  ins       = new TChildSession();

            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);
                m_ChildSessions.Add(ins);
            }
            finally
            {
                if (lt)
                    m_CreateSessionLock.Exit();
            }

            var ctx = new SessionCreateContext(this, ins, childType, data);
            await UniTask.RunOnThreadPool(CreateSession, ctx, cancellationToken: ReserveToken);

            return ins;
        }

        [ThreadSafe(Type = ThreadSafeAttribute.SafeType.SpinLock)]
        public async UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data)
            where TChildSession : IChildSession, new()
        {
            if (Disposed)
                throw new ObjectDisposedException(Type.Name);

            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type childType = typeof(TChildSession);
            var  ins       = new TChildSession();

            bool lt = false;
            try
            {
                m_CreateSessionLock.Enter(ref lt);
                m_ChildSessions.Add(ins);
            }
            finally
            {
                if (lt)
                    m_CreateSessionLock.Exit();
            }

            var ctx = new SessionCreateContext(this, ins, childType, data);
            await CreateSession(ctx).AttachExternalCancellation(ReserveToken);

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
                using var tempArray = TempArray<UniTask>.Shared(ctx.parentSession.ConnectedProviders.Count, true);
                using (DebugTimer.Start())
                {
                    int i = 0;
                    foreach (var item in ctx.parentSession.ConnectedProviders)
                    {
                        if (!item.Value.TryPeek(out var v)) continue;

                        var pType = item.Key;
                        // sessionConnector.Register(pType, v);

                        tempArray.Value[i++] = UniTask.RunOnThreadPool(RegisterProvider,
                            new ProviderRegistrationContext()
                            {
                                container    = sessionConnector,
                                providerType = pType,
                                provider     = v
                            });
                    }
                }
                await UniTask.WhenAll(tempArray.Value);
            }
            // else $"[Session: {Type.FullName}] No connector for {childType.FullName}".ToLog();

#if UNITY_EDITOR
            using (ctx.parentSession.m_SessionCreationBlock.Value = new SessionCreationBlock(ctx.sessionType))
#endif
            {
                await session.Initialize(ctx.parentSession.Owner, ctx.parentSession, ctx.data);
                EvaluateProviderSession(ctx.parentSession, ctx.sessionType);
            }
#if UNITY_EDITOR
            ctx.parentSession.m_SessionCreationBlock.Value = null;
#endif

            // $"[Session: {ctx.parentSession.Type.FullName}] created {ctx.sessionType.FullName}".ToLog();
        }

#line hidden
        [DebuggerHidden]
        [Conditional("UNITY_EDITOR")]
        private static void EvaluateProviderSession(ParentSession<TSessionData> s, Type childSessionType)
        {
#if UNITY_EDITOR
            if (s.m_SessionCreationBlock.Value is null) return;

            if (s.m_SessionCreationBlock.Value.RequestedProviderTypes.Count > 0)
            {
                string f = string.Join(
                    "\n",
                    s.m_SessionCreationBlock.Value.RequestedProviderTypes.Select(x => x.FullName)
                    );

                throw new InvalidOperationException(
                    $"Session({childSessionType.FullName}) has missing providers" +
                    $"which has provider that must be provided. "                 +
                    $"See {nameof(ProviderSessionAttribute)}.\n{f}");
            }
#endif
        }
#line default

        private static void RegisterProvider(object ctx)
        {
            ProviderRegistrationContext context = (ProviderRegistrationContext)ctx;

            context.container.Register(context.providerType, context.provider);
        }

        async UniTask IGameSessionCallback.OnSessionClose(IGameSessionBase child)
        {
            if (Disposed)
                throw new ObjectDisposedException(Type.Name);

            IChildSession session = (IChildSession)child;

            // Type childType = session.Type;

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

            // $"[Session: {Type.FullName}] closed {childType.FullName}".ToLog();
        }

        public async UniTask CloseAllSessions()
        {
            if (Disposed)
                throw new ObjectDisposedException(Type.Name);

            for (var i = m_ChildSessions.Count - 1; i >= 0; i--)
            {
                var session = m_ChildSessions[i];
                await session.Reserve();
            }
        }
        public async UniTask<IChildSession> WaitUntilSessionAvailableAsync(Type sessionType, float timeout)
        {
            if (Disposed)
                throw new ObjectDisposedException(Type.Name);

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
        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public IChildSession GetSession(Type sessionType)
        {
            if (Disposed)
                throw new ObjectDisposedException(Type.Name);

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
        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public TChildSession GetSession<TChildSession>()
        {
            return (TChildSession)GetSession(typeof(TChildSession));
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
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

#if UNITY_EDITOR
                if (m_SessionCreationBlock.Value is not null)
                    m_SessionCreationBlock.Value.RequestedProviderTypes.Remove(providerType);
#endif
            }
            finally
            {
                if (lt) m_CreateSessionLock.Exit();
            }
        }
        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
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