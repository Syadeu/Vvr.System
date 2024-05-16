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
using System.Reflection;
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

        /// <summary>
        /// Connects the specified connector to the given value.
        /// </summary>
        /// <param name="connectorType">The type of the connector.</param>
        /// <param name="connector">The connector object.</param>
        /// <param name="value">The value to connect to.</param>
        public static void Connect(Type connectorType, object connector, object value)
        {
            var methodInfo = connectorType.GetMethod(
                nameof(IConnector<ConnectorImpl>.Connect),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"{connectorType} not found");
            }

            methodInfo.Invoke(connector, new object[] { value });
        }

        /// <summary>
        /// Disconnects the specified connector from its associated object.
        /// </summary>
        /// <param name="connectorType">The type of the connector.</param>
        /// <param name="connector">The connector object.</param>
        /// <param name="value"></param>
        public static void Disconnect(Type connectorType, object connector, object value)
        {
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