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
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.Provider;

namespace Vvr.Controller.Session
{
    public abstract class RootSession
        : IParentSession, IChildSessionConnector,
            IGameSessionCallback,
            IDisposable
    {
        private          Type                        m_Type;
        private          Type[]                      m_ConnectorTypes;
        private readonly Dictionary<Type, IProvider> m_ConnectedProviders = new();

        private readonly List<IChildSession> m_ChildSessions = new();
        private          ConditionResolver   m_ConditionResolver;

        private Type Type => (m_Type ??= GetType());
        private Type[] ConnectorTypes
        {
            get
            {
                if (m_ConnectorTypes == null)
                {
                    var tp = typeof(IConnector<>);
                    m_ConnectorTypes =
                        Type.GetInterfaces()
                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == tp)
                            .ToArray();
                }

                return m_ConnectorTypes;
            }
        }

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

        public async UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data)
            where TChildSession : IChildSession
        {
            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type          childType = typeof(TChildSession);
            TChildSession session   = (TChildSession)Activator.CreateInstance(childType);

            IChildSessionConnector t = this;

            if (session is IChildSessionConnector sessionConnector)
            {
                $"[Session: {Type.FullName}] Chain connector to {childType.FullName}".ToLog();
                foreach (var item in m_ConnectedProviders)
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

        /// <summary>
        /// Creates a child session of type <paramref name="session"/> and initializes it with the specified session data.
        /// </summary>
        /// <param name="session">The type of child session to create.</param>
        /// <param name="data">The session data to initialize the child session with.</param>
        /// <returns>A UniTask representing the asynchronous operation. The task completes when the child session has been created and initialized.</returns>
        [PublicAPI]
        protected virtual UniTask OnCreateSession(IChildSession session) => UniTask.CompletedTask;

        /// <summary>
        /// Invoked when a child session is closed.
        /// </summary>
        /// <param name="session">The child session that was closed.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        [PublicAPI]
        protected virtual UniTask OnSessionClosed(IChildSession session) => UniTask.CompletedTask;

        async UniTask IGameSessionCallback.OnSessionClose(IGameSessionBase child)
        {
            IChildSession session = (IChildSession)child;

            await OnSessionClosed(session);

            m_ChildSessions.Remove(session);
        }

        protected virtual void Connect(ConditionResolver conditionResolver)
        {
        }

        public void Register<TProvider>(TProvider provider) where TProvider : IProvider
        {
            Type pType = typeof(TProvider);
            pType = Vvr.Provider.Provider.ExtractType(pType);

            IChildSessionConnector t = this;
            t.Register(pType, provider);
        }
        public void Unregister<TProvider>() where TProvider : IProvider
        {
            Type pType = typeof(TProvider);
            pType = Vvr.Provider.Provider.ExtractType(pType);

            IChildSessionConnector t = this;
            t.Unregister(pType);
        }

        public TProvider GetProvider<TProvider>() where TProvider : class, IProvider
        {
            TProvider r = null;
            foreach (var s in ChildSessions)
            {
                r = s.GetProvider<TProvider>();
                if (r != null) break;
            }

            return r;
        }

        private readonly Dictionary<Type, LinkedList<ConnectorReflectionUtils.Wrapper>>
            m_ConnectorWrappers = new();

        public void Connect<TProvider>(IConnector<TProvider> c) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = Vvr.Provider.Provider.ExtractType(t);

            if (!m_ConnectorWrappers.TryGetValue(t, out var list))
            {
                list                   = new();
                m_ConnectorWrappers[t] = list;
            }

            uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));

            Assert.IsFalse(list.Contains(new(hash)));
            ConnectorReflectionUtils.Wrapper
                wr = new(hash, x =>
                {
                    if (x == null) c.Disconnect();
                    else c.Connect((TProvider)x);
                });
            list.AddLast(wr);
        }

        public void Disconnect<TProvider>(IConnector<TProvider> c) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = Vvr.Provider.Provider.ExtractType(t);

            if (!m_ConnectorWrappers.TryGetValue(t, out var list)) return;

            c.Disconnect();
            uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));
            list.Remove(new ConnectorReflectionUtils.Wrapper(hash));
        }

        void IChildSessionConnector.Register(Type pType, IProvider provider)
        {
            $"[Session: {Type.FullName}] Connectors {ConnectorTypes.Length}".ToLog();
            m_ConnectedProviders[pType] = provider;
            for (var i = 0; i < ConnectorTypes.Length; i++)
            {
                var connectorType = ConnectorTypes[i];
                // $"[Session:{Type.FullName}] Found {connectorType.FullName}".ToLog();
                if (connectorType.GetGenericArguments()[0] != pType)
                {
                    $"{connectorType.GetGenericArguments()[0].AssemblyQualifiedName} != {pType.AssemblyQualifiedName}"
                        .ToLog();
                    continue;
                }

                ConnectorReflectionUtils.Connect(connectorType, this, provider);
                break;
            }

            ConnectObservers(pType, provider);
            $"[Session:{Type.FullName}] Connected {pType.FullName}".ToLog();

            foreach (var childSession in ChildSessions)
            {
                if (childSession is not IChildSessionConnector c) continue;

                c.Register(pType, provider);
            }

            OnProviderRegistered(pType, provider);
        }
        void IChildSessionConnector.Unregister(Type pType)
        {
            if (m_ConnectedProviders == null) return;

            if (m_ConnectedProviders.Remove(pType))
            {
                for (var i = 0; i < ConnectorTypes.Length; i++)
                {
                    var connectorType = ConnectorTypes[i];
                    if (connectorType.GetGenericArguments()[0] != pType) continue;

                    ConnectorReflectionUtils.Disconnect(connectorType, this);
                    $"[Session:{Type.FullName}] Disconnected {pType.FullName}".ToLog();
                    break;
                }

                DisconnectObservers(pType);
            }

            OnProviderUnregistered(pType);
        }

        private void ConnectObservers(Type providerType, IProvider provider)
        {
            if (!m_ConnectorWrappers.TryGetValue(providerType, out var list)) return;

            foreach (var wr in list)
            {
                wr.setter(provider);
            }
        }

        private void DisconnectObservers(Type providerType)
        {
            if (!m_ConnectorWrappers.TryGetValue(providerType, out var list)) return;

            foreach (var wr in list)
            {
                wr.setter(null);
            }
        }

        private void OnProviderRegistered(Type providerType, IProvider provider)
        {
            foreach (var childSession in ChildSessions)
            {
                if (childSession is not IChildSessionConnector c) continue;

                c.Register(providerType, provider);
            }
        }
        private void OnProviderUnregistered(Type providerType)
        {
            foreach (var childSession in ChildSessions)
            {
                if (childSession is not IChildSessionConnector c) continue;

                c.Unregister(providerType);
            }
        }
    }
}