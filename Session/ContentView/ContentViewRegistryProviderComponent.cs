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
using Vvr.MPC.Session.ContentView.Research;
using Vvr.Session.ContentView.BattleSign;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Mainmenu;
using Vvr.Session.ContentView.WorldBackground;

namespace Vvr.Session.ContentView
{
    public sealed class ContentViewRegistryProviderComponent : MonoBehaviour, IContentViewRegistryProvider
    {
        [SerializeField, Required] private MainmenuViewProviderComponent   m_MainmenuViewProvider;
        [SerializeField, Required] private DialogueViewProviderComponent   m_DialogueViewProvider;
        [SerializeField, Required] private ResearchViewProviderComponent   m_ResearchViewProvider;
        [SerializeField, Required] private WorldBackgroundViewProvider     m_WorldBackgroundViewProvider;
        [SerializeField, Required] private BattleSignViewProviderComponent m_BattleSignViewProvider;

        IMainmenuViewProvider IContentViewRegistryProvider.      MainmenuViewProvider        => m_MainmenuViewProvider;
        IDialogueViewProvider IContentViewRegistryProvider.       DialogueViewProvider        => m_DialogueViewProvider;
        IResearchViewProvider IContentViewRegistryProvider.       ResearchViewProvider        => m_ResearchViewProvider;
        IWorldBackgroundViewProvider IContentViewRegistryProvider.WorldBackgroundViewProvider => m_WorldBackgroundViewProvider;
        IBattleSignViewProvider IContentViewRegistryProvider.     BattleSignViewProvider      => m_BattleSignViewProvider;

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