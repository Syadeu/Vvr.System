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

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Vvr.Provider.Component
{
    public sealed class ViewRegistryProviderComponent : MonoBehaviour, IViewRegistryProvider
    {
        [SerializeField, Required]     private EventViewProviderComponent            m_CardViewProvider;
        [SerializeField, Required]     private DialogueViewProviderComponent         m_DialogueViewProvider;
        [FormerlySerializedAs("m_TimelineNodeViewProvider")] [SerializeField, Required] private TimelineNodeViewProviderComponent m_TimelineNodeViewViewProvider;
        [SerializeField, Required]     private StageViewProviderComponent            m_StageViewProvider;

        IEventViewProvider IViewRegistryProvider.            CardViewProvider             => m_CardViewProvider;
        IDialogueViewProvider IViewRegistryProvider.         DialogueViewProvider         => m_DialogueViewProvider;
        IEventTimelineNodeViewProvider IViewRegistryProvider.TimelineNodeViewViewProvider => m_TimelineNodeViewViewProvider;
        IStageViewProvider IViewRegistryProvider.            StageViewProvider            => m_StageViewProvider;

        private void Awake()
        {
            Provider.Static.Register<IViewRegistryProvider>(this);
        }
        private void OnDestroy()
        {
            Provider.Static.Unregister<IViewRegistryProvider>(this);
        }
    }
}