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
// File created : 2024, 05, 14 19:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Utility class for internal use that resolves IConnector interface methods.
    /// </summary>
    internal static class ConnectorReflectionUtils
    {
        public readonly struct Wrapper : IEquatable<Wrapper>
        {
            private readonly uint              m_Hash;
            public readonly  Action<IProvider> connect;
            public readonly  Action<IProvider> disconnect;

            public Wrapper(uint h)
            {
                m_Hash       = h;
                connect    = null;
                disconnect = null;
            }
            public Wrapper(uint h, Action<IProvider> con, Action<IProvider> discon)
            {
                m_Hash       = h;
                connect    = con;
                disconnect = discon;
            }

            public bool Equals(Wrapper other)
            {
                return m_Hash == other.m_Hash;
            }
        }
        struct ConnectorImpl : IProvider, IConnector<ConnectorImpl>
        {
            public void Connect(ConnectorImpl t)
            {
                throw new NotImplementedException();
            }

            public void Disconnect(ConnectorImpl t)
            {
                throw new NotImplementedException();
            }
        }

        private static readonly Type s_ConnectorGenericType = typeof(IConnector<>);

        private static readonly ThreadLocal<object[]>  s_MethodParameter      = new(() => new object[1]);

        private static          SemaphoreSlim            s_CachedTypeConnectorLock = new(1, 1);
        private static readonly Dictionary<Type, Type[]> s_CachedTypeConnectorMap  = new();

        public static void Purge()
        {
            s_CachedTypeConnectorMap.Clear();
        }

        public static bool TryGetConnectorType(Type t, Type providerType, out Type connectorType)
        {
            using var debugTimer = DebugTimer.StartWithCustomName(
                DebugTimer.BuildDisplayName(
                    nameof(ConnectorReflectionUtils),
                    nameof(TryGetConnectorType))
            );

            foreach (var type in GetConnectorTypes(t))
            {
                if (type.GetGenericArguments()[0] == providerType)
                {
                    connectorType = type;
                    return true;
                }
            }

            connectorType = null;
            return false;
        }
        public static IEnumerable<Type> GetConnectorTypes(Type t)
        {
            var interfaces = t.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var interfaceType = interfaces[i];
                if (!interfaceType.IsGenericType) continue;

                var p = interfaceType.GetGenericTypeDefinition();
                if (p == s_ConnectorGenericType) yield return interfaceType;
            }
        }

        /// <summary>
        /// Returns all the connector types implemented by the specified type.
        /// </summary>
        /// <param name="type">The type to get the connector types for.</param>
        /// <returns>All the connector types implemented by the specified type.</returns>
        public static IReadOnlyList<Type> GetAllConnectors([NotNull] Type type)
        {
            Assert.IsNotNull(type);

            List<Type> finalTypes;
            Type[]     interfaceTypes;
            using (var wl = new SemaphoreSlimLock(s_CachedTypeConnectorLock))
            {
                wl.Wait(TimeSpan.FromSeconds(1));

                if (s_CachedTypeConnectorMap.TryGetValue(type, out var cachedConnectorTypes))
                {
                    return cachedConnectorTypes;
                }

                InjectOptionsAttribute options = type.GetCustomAttribute<InjectOptionsAttribute>();
                interfaceTypes = type.GetInterfaces();
                if (options is not null && options.Cache)
                {
                    finalTypes = new();
                    for (int i = 0; i < interfaceTypes.Length; i++)
                    {
                        var e = interfaceTypes[i];

                        if (e.IsGenericType && e.GetGenericTypeDefinition() == s_ConnectorGenericType)
                        {
                            finalTypes.Add(e);
                        }
                    }

                    cachedConnectorTypes           = finalTypes.ToArray();
                    s_CachedTypeConnectorMap[type] = cachedConnectorTypes;

                    return cachedConnectorTypes;
                }
            }

            finalTypes = new();
            for (int i = 0; i < interfaceTypes.Length; i++)
            {
                var e = interfaceTypes[i];

                if (e.IsGenericType && e.GetGenericTypeDefinition() == s_ConnectorGenericType)
                {
                    finalTypes.Add(e);
                }
            }

            return finalTypes;
        }

        [ThreadStatic]
        private static Type s_PreviousConnectType;
        [ThreadStatic]
        private static MethodInfo s_PreviousConnectMethodInfo;

        /// <summary>
        /// Connects the specified connector to the given value.
        /// </summary>
        /// <param name="connectorType">The type of the connector.</param>
        /// <param name="connector">The connector object.</param>
        /// <param name="value">The value to connect to.</param>
        public static void Connect(Type connectorType, object connector, object value)
        {
            const string debugName = nameof(ConnectorReflectionUtils) + "." + nameof(Connect);
            using var    timer     = DebugTimer.StartWithCustomName(debugName);

            if (s_PreviousConnectType != connectorType ||
                s_PreviousConnectMethodInfo is null)
            {
                MethodInfo methodInfo = connectorType.GetMethod(
                    nameof(IConnector<ConnectorImpl>.Connect),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (methodInfo == null)
                {
                    throw new InvalidOperationException($"{connectorType} not found");
                }

                s_PreviousConnectType       = connectorType;
                s_PreviousConnectMethodInfo = methodInfo;
            }

            s_MethodParameter.Value[0] = value;
            s_PreviousConnectMethodInfo.Invoke(connector, s_MethodParameter.Value);
        }

        /// <summary>
        /// Disconnects the specified connector from its associated object.
        /// </summary>
        /// <param name="connectorType">The type of the connector.</param>
        /// <param name="connector">The connector object.</param>
        /// <param name="value"></param>
        public static void Disconnect(Type connectorType, object connector, object value)
        {
            const string debugName = nameof(ConnectorReflectionUtils) + "." + nameof(Disconnect);
            using var    timer     = DebugTimer.StartWithCustomName(debugName);

            var methodInfo = connectorType.GetMethod(
                nameof(IConnector<ConnectorImpl>.Disconnect),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"{connectorType} not found");
            }

            methodInfo.Invoke(connector, new object[] { value });
        }
    }
}