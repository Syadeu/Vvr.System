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
using System.Reflection;
using System.Threading;
using Cathei.BakingSheet.Internal;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.Provider;

namespace Vvr.Controller.Session
{
    /// <summary>
    /// Represents a child session.
    /// </summary>
    /// <typeparam name="TSessionData">The type of session data associated with the session.</typeparam>
    [Preserve, UnityEngine.Scripting.RequireDerived]
    public abstract class ChildSession<TSessionData> : IChildSession, IChildSessionConnector
        where TSessionData : ISessionData
    {
        private          Type                        m_Type;
        private          Type[]                      m_ConnectorTypes;
        private readonly Dictionary<Type, IProvider> m_ConnectedProviders = new();

        private CancellationTokenSource m_InitializeToken;

        private ConditionResolver m_ConditionResolver;

        protected internal IReadOnlyDictionary<Type, IProvider> ConnectedProviders => m_ConnectedProviders;

        protected Type Type => (m_Type ??= GetType());
        protected Type[] ConnectorTypes
        {
            get
            {
                if (m_ConnectorTypes == null)
                {
                    var tp = typeof(IConnector<>);
                    m_ConnectorTypes =
                        Type.GetInterfaces()
                            .Where(x=>x.IsGenericType && x.GetGenericTypeDefinition() == tp)
                            .ToArray();
                }

                return m_ConnectorTypes;
            }
        }

        public          Owner  Owner       { get; private set; }
        public abstract string DisplayName { get; }

        /// <summary>
        /// Represents the root parent session of a child session.
        /// </summary>
        /// <remarks>
        /// The root parent session is the topmost session in the hierarchy that does not have a parent session.
        /// It is obtained by traversing up the parent sessions until a session without a parent is found.
        /// </remarks>
        [PublicAPI]
        public IParentSession Root
        {
            get
            {
                IParentSession current = Parent;
                while (current is IChildSession { Parent: not null } childSession)
                {
                    current = childSession.Parent;
                }

                return current;
            }
        }
        [PublicAPI]
        public IParentSession Parent { get; private set; }

        /// <summary>
        /// Represents the session data associated with a child session.
        /// </summary>
        /// <typeparam name="TSessionData">The type of the session data.</typeparam>
        /// <remarks>
        /// The session data holds the information specific to a child session.
        /// It can be accessed and modified by the child session and its parent session.
        /// </remarks>
        [PublicAPI]
        public TSessionData   Data   { get; private set; }

        /// <summary>
        /// Represents a resolver used for resolving conditions in a session.
        /// </summary>
        public IReadOnlyConditionResolver ConditionResolver => m_ConditionResolver;

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>
        /// The Disposed property is used to determine whether an object has been disposed or not.
        /// When an object is disposed, it means that any unmanaged resources used by the object have been released and the object can no longer be used.
        /// This property is typically set to true in the Dispose method of the object.
        /// It is important to check the value of Disposed before using an object to avoid accessing a disposed object and causing any unexpected behavior or exceptions.
        /// </remarks>
        public bool Disposed { get; private set; }

        UniTask IGameSessionBase.Initialize(Owner owner)
        {
            Owner = owner;
            return UniTask.CompletedTask;
        }
        UniTask IChildSession.Initialize(IParentSession parent, ISessionData data)
        {
            ParentSessionAttribute att = GetType().GetCustomAttribute<ParentSessionAttribute>();
            if (att != null)
            {
                if (att.IncludeInherits)
                {
                    Assert.IsTrue(VvrTypeHelper.InheritsFrom(parent.GetType(), att.Type));
                }
                else Assert.AreEqual(att.Type, parent.GetType());
            }

            if (m_InitializeToken != null)
            {
                m_InitializeToken.Cancel();
                m_InitializeToken.Dispose();
            }

            Disposed = false;

            Parent = parent;
            Data   = data != null ? (TSessionData)data : default;

            m_ConditionResolver = Condition.ConditionResolver.Create(this, parent.ConditionResolver);
            Register(m_ConditionResolver);

            m_InitializeToken = new();

            return OnInitialize(parent, Data)
                .AttachExternalCancellation(m_InitializeToken.Token)
                .SuppressCancellationThrow()
                ;
        }

        /// <summary>
        /// Reserves the session.
        /// This method is used to release resources and prepare the session for disposal.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public async UniTask Reserve()
        {
            m_InitializeToken.Cancel();

            await OnReserve();

            if (Parent is IGameSessionCallback callback)
            {
                callback.OnSessionClose(this);
            }

            m_InitializeToken.Dispose();
            m_ConditionResolver.Dispose();

            Parent              = null;
            Data                = default;
            m_ConditionResolver = null;
            m_InitializeToken   = null;

            Disposed = true;
        }

        /// <summary>
        /// Initializes the ChildSession with the specified parent session and data.
        /// This method is called when the ChildSession is being initialized. It sets the Parent, Data, ConditionResolver, and initializes the session.
        /// </summary>
        /// <param name="session">The parent session.</param>
        /// <param name="data">The session data.</param>
        /// <returns>A UniTask representing the asynchronous initialization operation.</returns>
        [PublicAPI]
        protected virtual UniTask OnInitialize(IParentSession session, TSessionData data) => UniTask.CompletedTask;

        /// <summary>
        /// Reserves the session.
        /// This method is used to release resources and prepare the session for disposal.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        [PublicAPI]
        protected virtual UniTask OnReserve() => UniTask.CompletedTask;

        protected virtual void Register(ConditionResolver conditionResolver) {}

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
            if (this is TProvider p) return p;

            TProvider result = null;
            if (this is IParentSession parentSession)
            {
                foreach (var s in parentSession.ChildSessions)
                {
                    result = s.GetProvider<TProvider>();
                    if (result != null) break;
                }
            }

            return result;
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

        protected virtual void OnProviderRegistered(Type   providerType, IProvider provider) {}
        protected virtual void OnProviderUnregistered(Type providerType) {}
    }
}