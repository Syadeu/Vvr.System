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
using Vvr.MPC.Provider;

namespace Vvr.Controller.Session
{
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