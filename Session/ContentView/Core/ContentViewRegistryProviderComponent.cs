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
// File created : 2024, 05, 23 23:05

#endregion

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView
{
    /// <summary>
    /// Represents a component that provides a registry of content view providers.
    /// </summary>
    [HideMonoScript]
    [DisallowMultipleComponent]
    internal sealed class ContentViewRegistryProviderComponent : MonoBehaviour, IContentViewRegistryProvider
    {
#if UNITY_EDITOR
        [OnValueChanged(nameof(OnProviderComponentArrayChanged))]
        [InfoBox(
            "Duplicated provider. This is not allowed.", InfoMessageType.Error,
            VisibleIf = nameof(HasDuplicatedProvider))]
        [ChildGameObjectsOnly]
#endif
        [SerializeField, Required] private ContentViewProviderComponent[] m_ProviderComponents;

        private readonly Dictionary<Type, IContentViewProvider> m_Providers = new();

        IReadOnlyDictionary<Type, IContentViewProvider> IContentViewRegistryProvider.Providers => m_Providers;

#if UNITY_EDITOR

        private readonly Dictionary<Type, ContentViewProviderComponent>
            m_CachedProviderComponents = new();

        private bool HasDuplicatedProvider { get; set; }

        private void OnProviderComponentArrayChanged()
        {
            m_CachedProviderComponents.Clear();
            for (int i = 0; i < m_ProviderComponents.Length; i++)
            {
                var e = m_ProviderComponents[i];
                if (!m_CachedProviderComponents.TryAdd(e.ProviderType, e))
                {
                    HasDuplicatedProvider = true;
                    break;
                }
            }

            HasDuplicatedProvider = false;
        }
#endif

        private void Awake()
        {
            for (int i = 0; i < m_ProviderComponents.Length; i++)
            {
                var e = m_ProviderComponents[i];
                m_Providers[e.EventType] = e;
            }

            Vvr.Provider.Provider.Static.Register<IContentViewRegistryProvider>(this);
        }
        private void OnDestroy()
        {
            Vvr.Provider.Provider.Static.Unregister<IContentViewRegistryProvider>(this);
        }

        IContentViewProvider IContentViewRegistryProvider.Resolve<TEvent>()
        {
            Type t = VvrTypeHelper.TypeOf<TEvent>.Type;

            return m_Providers[t];
        }
    }
}