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

using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Research;

namespace Vvr.Session.ContentView
{
    public sealed class ContentViewRegistryProviderComponent : MonoBehaviour, IContentViewRegistryProvider
    {
        [SerializeField, Required] private DialogueViewProviderComponent m_DialogueViewProvider;
        [SerializeField, Required] private ResearchViewProviderComponent m_ResearchViewProvider;

        IDialogueViewProvider IContentViewRegistryProvider.DialogueViewProvider => m_DialogueViewProvider;
        IResearchViewProvider IContentViewRegistryProvider. ResearchViewProvider => m_ResearchViewProvider;

        private void Awake()
        {
            Vvr.Provider.Provider.Static.Register<IContentViewRegistryProvider>(this);
        }

        private void OnDestroy()
        {
            Vvr.Provider.Provider.Static.Unregister<IContentViewRegistryProvider>(this);
        }
    }
}