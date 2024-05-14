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
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Session
{
    public abstract class ParentSession<TSessionData>
        : ChildSession<TSessionData>, IParentSession, IGameSessionCallback,
            IParentSessionConnector

        where TSessionData : ISessionData
    {
        private readonly List<IChildSession> m_ChildSessions  = new();


        public  IReadOnlyList<IChildSession> ChildSessions => m_ChildSessions;

        protected override UniTask OnReserve()
        {
            m_ChildSessions.Clear();

            return base.OnReserve();
        }

        [MustUseReturnValue]
        public async UniTask<TChildSession> CreateSession<TChildSession>(ISessionData data)
            where TChildSession : IChildSession
        {
            Assert.IsFalse(VvrTypeHelper.TypeOf<TChildSession>.IsAbstract);

            Type          childType = typeof(TChildSession);
            TChildSession session   = (TChildSession)Activator.CreateInstance(childType);

            IChildSessionConnector t = this;

            if (t.ConnectedProviders != null &&
                session is IChildSessionConnector sessionConnector)
            {
                $"[Session: {Type.FullName}] Chain connector to {childType.FullName}".ToLog();
                for (int i = 0; i < t.ConnectedProviders.Length; i++)
                {
                    var provider = t.ConnectedProviders[i];
                    if (provider == null) continue;

                    var pType = ConnectorTypes[i];
                    sessionConnector.Connect(pType.GetGenericArguments()[0], provider);
                }
            }
            else $"[Session: {Type.FullName}] No connector for {childType.FullName}".ToLog();

            await session.Initialize(Owner);
            m_ChildSessions.Add(session);

            await OnCreateSession(session);

            await session.Initialize(this, data);

            return session;
        }
        async UniTask IGameSessionCallback.OnSessionClosed(IGameSessionBase child)
        {
            IChildSession session = (IChildSession)child;

            await OnSessionClosed(session);
            m_ChildSessions.Remove(session);
        }

        public void Connect<TProvider>(TProvider provider) where TProvider : IProvider
        {
            Type pType = typeof(TProvider);
            pType = MPC.Provider.Provider.ExtractType(pType);

            IChildSessionConnector t = this;
            t.Connect(pType, provider);
        }
        public void Disconnect<TProvider>() where TProvider : IProvider
        {
            Type pType = typeof(TProvider);
            pType = MPC.Provider.Provider.ExtractType(pType);

            IChildSessionConnector t = this;
            t.Disconnect(pType);
        }

        protected virtual UniTask OnCreateSession(IChildSession session) => UniTask.CompletedTask;
        protected virtual UniTask OnSessionClosed(IChildSession session) => UniTask.CompletedTask;
    }

    public interface IParentSessionConnector
    {
        void Connect<TProvider>(TProvider provider) where TProvider : IProvider;
    }

    internal static class ConnectorReflectionUtils
    {
        struct ConnectorImpl : IProvider, IConnector<ConnectorImpl>
        {
            public void Connect(ConnectorImpl t)
            {
                throw new NotImplementedException();
            }

            public void Disconnect()
            {
                throw new NotImplementedException();
            }
        }

        public static void Connect(Type connectorType, object connector, object value)
        {
            var methodInfo = connectorType.GetMethod(
                nameof(IConnector<ConnectorImpl>.Connect),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo == null)
            {
                $"{connectorType} not found".ToLogError();
                return;
            }

            methodInfo.Invoke(connector, new object[] { value });
        }
        public static void Disconnect(Type connectorType, object connector)
        {
            var methodInfo = connectorType.GetMethod(
                nameof(IConnector<ConnectorImpl>.Disconnect),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo == null)
            {
                $"{connectorType} not found".ToLogError();
                return;
            }

            methodInfo.Invoke(connector, null);
        }
    }
}