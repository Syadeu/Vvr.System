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
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Session
{
    [Preserve, UnityEngine.Scripting.RequireDerived]
    public abstract class ChildSession<TSessionData> : IChildSession, IChildSessionConnector
        where TSessionData : ISessionData
    {
        private Type   m_Type;
        private Type[] m_ConnectorTypes;

        private CancellationTokenSource m_InitializeToken;

        private ConditionResolver m_ConditionResolver;

        protected internal Type Type => (m_Type ??= GetType());
        protected internal Type[] ConnectorTypes
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
        public TSessionData   Data   { get; private set; }

        public IReadOnlyConditionResolver ConditionResolver => m_ConditionResolver;

        public bool Disposed { get; private set; }

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
            Connect(m_ConditionResolver);

            m_InitializeToken = new();

            return OnInitialize(parent, Data)
                .AttachExternalCancellation(m_InitializeToken.Token)
                .SuppressCancellationThrow()
                ;
        }
        UniTask IGameSessionBase.Initialize(Owner owner)
        {
            Owner = owner;
            return UniTask.CompletedTask;
        }

        public async UniTask Reserve()
        {
            m_InitializeToken.Cancel();

            await OnReserve();

            if (Parent is IGameSessionCallback callback)
            {
                callback.OnSessionClosed(this);
            }

            m_InitializeToken.Dispose();
            m_ConditionResolver.Dispose();

            Parent              = null;
            Data                = default;
            m_ConditionResolver = null;
            m_InitializeToken   = null;

            Disposed = true;
        }

        protected virtual UniTask OnInitialize(IParentSession session, TSessionData data) => UniTask.CompletedTask;
        protected virtual UniTask OnReserve() => UniTask.CompletedTask;

        protected virtual void Connect(ConditionResolver conditionResolver) {}

        private IProvider[] m_ConnectedProviders;

        IProvider[] IChildSessionConnector.ConnectedProviders => m_ConnectedProviders;

        void IChildSessionConnector.Connect(Type pType, IProvider provider)
        {
            m_ConnectedProviders ??= new IProvider[ConnectorTypes.Length];

            $"[Session: {Type.FullName}] Connectors {ConnectorTypes.Length}".ToLog();
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
                m_ConnectedProviders[i] = provider;
                $"[Session:{Type.FullName}] Connected {pType.FullName}".ToLog();
                break;
            }

            if (this is not IParentSession parentSession)
            {
                return;
            }
            foreach (var childSession in parentSession.ChildSessions)
            {
                if (childSession is not IChildSessionConnector c) continue;

                c.Connect(pType, provider);
            }
        }

        void IChildSessionConnector.Disconnect(Type pType)
        {
            if (m_ConnectedProviders == null) return;

            for (var i = 0; i < ConnectorTypes.Length; i++)
            {
                var connectorType = ConnectorTypes[i];
                if (connectorType.GetGenericArguments()[0] != pType) continue;

                ConnectorReflectionUtils.Disconnect(connectorType, this);
                m_ConnectedProviders[i] = null;
                $"[Session:{Type.FullName}] Disconnected {pType.FullName}".ToLog();
                break;
            }

            if (this is not IParentSession parentSession)
            {
                return;
            }

            foreach (var childSession in parentSession.ChildSessions)
            {
                if (childSession is not IChildSessionConnector c) continue;

                c.Disconnect(pType);
            }
        }
    }

    internal interface IChildSessionConnector
    {
        IProvider[] ConnectedProviders { get; }

        void Connect(Type pType, IProvider provider);
        void Disconnect(Type pType);
    }
}