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
// File created : 2024, 05, 18 00:05

#endregion

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.EventView.Core
{
    [HideMonoScript]
    [DisallowMultipleComponent]
    internal sealed class ViewRegistryProviderComponent : MonoBehaviour, IViewRegistryProvider
    {
        [SerializeField, Required] private EventViewProviderComponent[] m_ProviderComponents;

        private readonly Dictionary<Type, IEventViewProvider> m_Providers = new();

        IReadOnlyDictionary<Type, IEventViewProvider> IViewRegistryProvider.Providers => m_Providers;

        private void Awake()
        {
            for (int i = 0; i < m_ProviderComponents.Length; i++)
            {
                var e = m_ProviderComponents[i];
                m_Providers[e.ProviderType] = e;
            }

            Vvr.Provider.Provider.Static.Register<IViewRegistryProvider>(this);
        }
        private void OnDestroy()
        {
            Vvr.Provider.Provider.Static.Unregister<IViewRegistryProvider>(this);
        }

        public IProvider Resolve(Type providerType)
        {
            if (m_Providers.TryGetValue(providerType, out var p))
                return p;

            for (int i = 0; i < m_ProviderComponents.Length; i++)
            {
                var  e = m_ProviderComponents[i];
                Type t = e.GetType();

                if (VvrTypeHelper.InheritsFrom(t, providerType))
                {
                    return e;
                }
            }

            throw new InvalidOperationException();
        }
        public TProvider Resolve<TProvider>()
        {
            for (int i = 0; i < m_ProviderComponents.Length; i++)
            {
                var  e = m_ProviderComponents[i];
                if (e is TProvider p) return p;
            }

            throw new InvalidOperationException();
        }

#if UNITY_EDITOR

        [Button(DirtyOnClick = true)]
        private void AddProviders()
        {
            List<EventViewProviderComponent> list  = new();

            int count = transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var e = transform.GetChild(i);

                if (!e.TryGetComponent(out EventViewProviderComponent com)) continue;

                list.Add(com);
            }

            m_ProviderComponents = list.ToArray();
        }

#endif
    }
}