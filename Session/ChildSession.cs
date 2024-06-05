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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Vvr.Session
{
    /// <summary>
    /// Represents a child session.
    /// </summary>
    /// <typeparam name="TSessionData">The type of session data associated with the session.</typeparam>
    [Preserve, UnityEngine.Scripting.RequireDerived]
    public abstract class ChildSession<TSessionData> : IChildSession, IDisposable
        where TSessionData : ISessionData
    {
        private          Type                        m_Type;
        private          Type[]                      m_ConnectorTypes;
        private readonly Dictionary<Type, IProvider> m_ConnectedProviders = new();

        private readonly Dictionary<Type, LinkedList<ConnectorReflectionUtils.Wrapper>>
            m_ConnectorWrappers = new();

        private CancellationTokenSource m_ReserveTokenSource;

        private ConditionResolver m_ConditionResolver;
        private TSessionData      m_SessionData;
        private bool              m_Initialized;

        protected IReadOnlyDictionary<Type, IProvider> ConnectedProviders => m_ConnectedProviders;

        public Type Type => (m_Type ??= GetType());
        private Type[] ConnectorTypes
        {
            get
            {
                if (m_ConnectorTypes == null)
                {
                    // var tp = typeof(IConnector<>);
                    // m_ConnectorTypes =
                    //     Type.GetInterfaces()
                    //         .Where(x=>x.IsGenericType && x.GetGenericTypeDefinition() == tp)
                    //         .ToArray();

                    m_ConnectorTypes = ConnectorReflectionUtils.GetAllConnectors(Type).ToArray();
                }

                return m_ConnectorTypes;
            }
        }

        public          Owner  Owner       { get; private set; }
        public abstract string DisplayName { get; }

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
        public TSessionData Data
        {
            get
            {
                if (!m_Initialized)
                    throw new InvalidOperationException("You are trying to access session data before initialized");
                return m_SessionData;
            }
            private set => m_SessionData = value;
        }

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

        /// <summary>
        /// Represents the token used for reserving resources in a session.
        /// </summary>
        /// <remarks>
        /// The ReserveToken property provides a cancellation token that can be used to reserve resources in a session.
        /// It is accessible in child sessions that derive from the ChildSession base class.
        /// The ReserveToken is obtained from a CancellationTokenSource and can be used to request cancellation of the operation.
        /// </remarks>
        [PublicAPI]
        protected CancellationToken ReserveToken => m_ReserveTokenSource.Token;

        UniTask IGameSessionBase.Initialize(Owner owner, IParentSession parent, ISessionData data)
        {
            Owner = owner;

            EvaluateSessionCreation(parent);

            if (m_ReserveTokenSource != null)
            {
                m_ReserveTokenSource.Cancel();
                m_ReserveTokenSource.Dispose();
            }

            Disposed = false;

            Parent = parent;
            Data   = data != null ? (TSessionData)data : default;

            m_Initialized = true;

            m_ConditionResolver = Controller.Condition.ConditionResolver.Create(this, parent?.ConditionResolver);
            SetupConditionResolver(m_ConditionResolver);

            m_ReserveTokenSource = new();

            return OnInitialize(parent, Data)
                .AttachExternalCancellation(m_ReserveTokenSource.Token)
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
            m_ReserveTokenSource.Cancel();

            await OnReserve();

            DisconnectAllObservers();
            UnregisterAll();

            if (Parent is IGameSessionCallback callback)
            {
                callback.OnSessionClose(this);
            }

            m_ConnectedProviders.Clear();
            m_ConnectorWrappers.Clear();

            m_ReserveTokenSource.Dispose();
            m_ConditionResolver.Dispose();

            Parent               = null;
            Data                 = default;
            m_ConditionResolver  = null;
            m_ReserveTokenSource = null;

            m_Initialized = false;
            Disposed      = true;
            // TODO: recycling
            ((IDisposable)this).Dispose();
        }

        void IDisposable.Dispose()
        {
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

        #region IDependencyContainer

        public IDependencyContainer Register(Type pType, IProvider provider)
        {
            EvaluateProviderRegistration(pType, provider);

            // $"[Session: {Type.FullName}] Connectors {ConnectorTypes.Length}".ToLog();
            m_ConnectedProviders[pType] = provider;
            {
                foreach (var connectorType in ConnectorTypes)
                {
                    // $"[Session:{Type.FullName}] Found {connectorType.FullName}".ToLog();
                    if (connectorType.GetGenericArguments()[0] != pType)
                    {
                        // $"{connectorType.GetGenericArguments()[0].AssemblyQualifiedName} != {pType.AssemblyQualifiedName}"
                        // .ToLog();
                        continue;
                    }

                    ConnectorReflectionUtils.Connect(connectorType, this, provider);
                    break;
                }
            }

            ConnectObservers(pType, provider);
            // $"[Session:{Type.FullName}] Connected {pType.FullName}".ToLog();

            OnProviderRegistered(pType, provider);
            return this;
        }
        public IDependencyContainer Unregister(Type pType)
        {
            if (m_ConnectedProviders == null) return this;

            if (m_ConnectedProviders.Remove(pType, out var provider))
            {
                for (var i = 0; i < ConnectorTypes.Length; i++)
                {
                    var connectorType = ConnectorTypes[i];
                    if (connectorType.GetGenericArguments()[0] != pType) continue;

                    ConnectorReflectionUtils.Disconnect(connectorType, this, provider);
                    $"[Session:{Type.FullName}] Disconnected {pType.FullName}".ToLog();
                    break;
                }

                DisconnectObservers(pType);
            }

            OnProviderUnregistered(pType);
            return this;
        }
        public IDependencyContainer Register<TProvider>(TProvider provider) where TProvider : IProvider
        {
            Type pType = typeof(TProvider);
            return Register(pType, provider);
        }
        public IDependencyContainer Unregister<TProvider>() where TProvider : IProvider
        {
            Type pType = typeof(TProvider);
            return Unregister(pType);
        }

        public IProvider GetProviderRecursive(Type providerType)
        {
            const string debugName  = "ChildSession.GetProviderRecursive";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            if (providerType is null)
                throw new ArgumentException(nameof(providerType));
            if (!VvrTypeHelper.InheritsFrom<IProvider>(providerType))
                throw new InvalidOperationException("Type must " + nameof(IProvider));

            if (VvrTypeHelper.InheritsFrom<IProvider>(Type)) return (IProvider)this;

            foreach (var injectedProvider in m_ConnectedProviders)
            {
                if (VvrTypeHelper.InheritsFrom(providerType, injectedProvider.Key))
                {
                    return injectedProvider.Value;
                }
            }

            IProvider result = null;
            if (this is IParentSession parentSession)
            {
                foreach (var s in parentSession.ChildSessions)
                {
                    result = s.GetProviderRecursive(providerType);
                    if (result != null) break;
                }
            }

            return result;
        }
        public TProvider GetProviderRecursive<TProvider>() where TProvider : class, IProvider
        {
            const string debugName  = "ChildSession.GetProviderRecursive<TProvider>";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            if (this is TProvider p) return p;
            foreach (var injectedProvider in m_ConnectedProviders.Values)
            {
                if (injectedProvider is TProvider r) return r;
            }

            TProvider result = null;
            if (this is IParentSession parentSession)
            {
                foreach (var s in parentSession.ChildSessions)
                {
                    result = s.GetProviderRecursive<TProvider>();
                    if (result != null) break;
                }
            }

            return result;
        }

        public IDependencyContainer Connect<TProvider>(IConnector<TProvider> c) where TProvider : IProvider
        {
            const string debugName  = "ChildSession.Connect<TProvider>";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            Assert.IsNotNull(c);
            if (ReferenceEquals(this, c))
                throw new InvalidOperationException("cannot connect self");

            Type t = typeof(TProvider);
            t = Vvr.Provider.Provider.ExtractType(t);

            Assert.IsNotNull(t);

            if (!m_ConnectorWrappers.TryGetValue(t, out var list))
            {
                list                   = new();
                m_ConnectorWrappers[t] = list;
            }

            uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));

            Assert.IsFalse(list.Contains(new(hash)));
            ConnectorReflectionUtils.Wrapper
                wr = new(hash,
                    x => c.Connect((TProvider)x),
                    x => c.Disconnect((TProvider)x));
            list.AddLast(wr);

            if (m_ConnectedProviders.TryGetValue(t, out var provider))
            {
                c.Connect((TProvider)provider);
            }

            return this;
        }
        public IDependencyContainer Disconnect<TProvider>(IConnector<TProvider> c) where TProvider : IProvider
        {
            const string debugName  = "ChildSession.Disconnect<TProvider>";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            Type t = typeof(TProvider);
            t = Vvr.Provider.Provider.ExtractType(t);

            if (!m_ConnectorWrappers.TryGetValue(t, out var list)) return this;

            if (m_ConnectedProviders.TryGetValue(t, out var provider))
                c.Disconnect((TProvider)provider);

            uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));
            bool result = list.Remove(new ConnectorReflectionUtils.Wrapper(hash));
            Assert.IsTrue(result);

            return this;
        }

        bool IDependencyContainer.TryGetProvider(Type providerType, out IProvider provider)
        {
            return m_ConnectedProviders.TryGetValue(providerType, out provider);
        }

        IEnumerable<KeyValuePair<Type, IProvider>> IDependencyContainer.GetEnumerable()
        {
            return m_ConnectedProviders;
        }

        #endregion

        /// <summary>
        /// Unregisters all connected providers from the child session.
        /// </summary>
        private void UnregisterAll()
        {
            IDependencyContainer t = this;
            foreach (var pType in m_ConnectedProviders.Keys.ToArray())
            {
                t.Unregister(pType);
            }
            m_ConnectedProviders.Clear();
        }

        /// <summary>
        /// Connects observers to the specified provider.
        /// </summary>
        /// <param name="providerType">The type of the provider.</param>
        /// <param name="provider">The provider to connect observers to.</param>
        private void ConnectObservers(Type providerType, IProvider provider)
        {
            if (!m_ConnectorWrappers.TryGetValue(providerType, out var list)) return;

            foreach (var wr in list)
            {
                wr.connect(provider);
            }
        }

        /// <summary>
        /// Disconnects the observers associated with the specified provider type.
        /// </summary>
        /// <param name="providerType">The type of the provider.</param>
        private void DisconnectObservers(Type providerType)
        {
            if (!m_ConnectorWrappers.TryGetValue(providerType, out var list)) return;

            if (!m_ConnectedProviders.TryGetValue(providerType, out var provider)) return;

            foreach (var wr in list)
            {
                wr.disconnect(provider);
            }
        }
        /// <summary>
        /// Disconnects all observers from the child session.
        /// This method removes all observers that are currently connected to the child session.
        /// </summary>
        private void DisconnectAllObservers()
        {
            foreach (var list in m_ConnectorWrappers)
            {
                if (!m_ConnectedProviders.TryGetValue(list.Key, out var provider)) continue;

                foreach (var wr in list.Value)
                {
                    wr.disconnect(provider);
                }
                list.Value.Clear();
            }

            m_ConnectorWrappers.Clear();
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        protected virtual void EvaluateSessionCreation(IParentSession parent)
        {
            ParentSessionAttribute att = Type.GetCustomAttribute<ParentSessionAttribute>();
            if (att != null)
            {
                if (parent == null)
                    throw new InvalidOperationException(
                        $"Session({Type.FullName}) is trying to create without any parent " +
                        $"while its child marked by attribute.");

                Type parentType = parent.Type;
                if (att.IncludeInherits)
                {
                    if (!VvrTypeHelper.InheritsFrom(parentType, att.Type))
                        throw new InvalidOperationException(
                            $"Session({Type.FullName}) trying to create under " +
                            $"{parentType.FullName} is not inherits from {att.Type.FullName}");
                }
                else
                {
                    if (att.Type != parentType)
                        throw new InvalidOperationException(
                            $"Session({Type.FullName}) trying to create under " +
                            $"{parentType.FullName} but only accepts {att.Type.FullName}");
                }
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        protected virtual void EvaluateProviderRegistration(Type providerType, IProvider provider)
        {
            if (m_ConnectedProviders.TryGetValue(providerType, out var existing) &&
                !ReferenceEquals(existing, provider))
                throw new InvalidOperationException($"Already registered {providerType.FullName}.");
        }

        protected virtual void SetupConditionResolver(ConditionResolver conditionResolver) {}

        /// <summary>
        /// This method is called when a provider is registered with the child session.
        /// </summary>
        /// <param name="providerType">The type of the provider.</param>
        /// <param name="provider">The instance of the provider.</param>
        protected virtual void OnProviderRegistered(Type   providerType, IProvider provider) {}
        /// <summary>
        /// Invoked when a provider is unregistered from the session.
        /// </summary>
        /// <param name="providerType">The type of the provider being unregistered.</param>
        protected virtual void OnProviderUnregistered(Type providerType) {}
    }
}