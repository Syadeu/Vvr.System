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
// File created : 2024, 06, 05 11:06

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// The DependencyInjector class is responsible for injecting dependencies into game objects by connecting them with the appropriate providers.
    /// </summary>
    [DisallowMultipleComponent]
    [HideMonoScript]
    internal sealed class DependencyInjector : MonoBehaviour
    {
        [ChildGameObjectsOnly] [SerializeField, Required]
        private GameObject[] m_Objects;

        internal void Inject(Type providerType, IProvider provider)
        {
            Type connectorType = ConnectorReflectionUtils.GetConnectorType(providerType);

            for (int i = 0; i < m_Objects.Length; i++)
            {
                var e = m_Objects[i];
                if (!e.TryGetComponent(connectorType, out var connector)) continue;

                ConnectorReflectionUtils.Connect(connectorType, connector, provider);
            }
        }

#if UNITY_EDITOR
        [PropertySpace]
        [Button(DirtyOnClick = true)]
        private void RegisterAllConnectors()
        {
            HashSet<GameObject> list = new();
            foreach (var com in GetComponentsInChildren(VvrTypeHelper.TypeOf<MonoBehaviour>.Type))
            {
                if (!ConnectorReflectionUtils.GetAllConnectors(com.GetType()).Any())
                {
                    continue;
                }

                list.Add(com.gameObject);
            }

            m_Objects = list.ToArray();
        }
#endif
    }
}