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
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// The DependencyInjector class is responsible for injecting dependencies into game objects by connecting them with the appropriate providers.
    /// </summary>
    [DisallowMultipleComponent]
    [HideMonoScript]
    [InfoBox(
        "The DependencyInjector is responsible for injecting dependencies into game objects " +
        "by connecting them with the appropriate providers.")]
    internal sealed class DependencyInjector : MonoBehaviour
    {
        [ChildGameObjectsOnly] [SerializeField, Required]
        private GameObject[] m_Objects;

        /// <summary>
        /// Injects dependencies into game objects by connecting them with the appropriate providers.
        /// </summary>
        /// <param name="providerType">The type of the provider.</param>
        /// <param name="provider">The provider instance.</param>
        internal void Inject([NotNull] Type providerType, [NotNull] IProvider provider)
        {
            for (int i = 0; i < m_Objects.Length; i++)
            {
                var             e          = m_Objects[i];
                List<Component> components = ListPool<Component>.Get();
                e.GetComponents(components);

                foreach (var com in components)
                {
                    var comT = com.GetType();
                    if (!ConnectorReflectionUtils.TryGetConnectorType(comT, providerType, out var connectorType))
                    {
                        continue;
                    }

                    ConnectorReflectionUtils.Connect(connectorType, com, provider);
                }

                components.Clear();
                ListPool<Component>.Release(components);
            }
        }

        /// <summary>
        /// Detaches dependencies from game objects by disconnecting them from the specified provider.
        /// </summary>
        /// <param name="providerType">The type of the provider.</param>
        /// <param name="provider">The provider instance.</param>
        internal void Detach([NotNull] Type providerType, [NotNull] IProvider provider)
        {
            for (int i = 0; i < m_Objects.Length; i++)
            {
                var             e          = m_Objects[i];
                List<Component> components = ListPool<Component>.Get();
                e.GetComponents(components);

                foreach (var com in components)
                {
                    var comT = com.GetType();
                    if (!ConnectorReflectionUtils.TryGetConnectorType(comT, providerType, out var connectorType))
                    {
                        continue;
                    }
                    ConnectorReflectionUtils.Disconnect(connectorType, com, provider);
                }

                components.Clear();
                ListPool<Component>.Release(components);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Registers all connectors in the game objects by adding them to the list of objects.
        /// </summary>
        /// <remarks>
        /// This method is responsible for finding all game objects that have connectors attached to them and adding them to the list of objects in the <see cref="DependencyInjector"/> class.
        /// </remarks>
        [PropertySpace]
        [Button(DirtyOnClick = true)]
        [DetailedInfoBox(
            "Registers all connectors in the game objects by adding them to the list of objects.",
            "This method is responsible for finding all game objects that have connectors attached to them " +
            "and adding them to the list of objects in the \"DependencyInjector\" class.")]
        private void RegisterAllConnectors(bool includeInactive = false)
        {
            HashSet<GameObject> list = new();
            foreach (var com in GetComponentsInChildren(VvrTypeHelper.TypeOf<MonoBehaviour>.Type, includeInactive))
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