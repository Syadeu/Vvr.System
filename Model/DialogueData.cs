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
// File created : 2024, 05, 21 09:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Vvr.Model
{
    [CreateAssetMenu(menuName = "Vvr/Create DialogueData", fileName = "DialogueData", order = 0)]
    public class DialogueData : ScriptableObject, IDialogueData
    {
        [SerializeField] private int    m_Index;

        [SerializeField] private DialogueSpeaker[]    m_Speakers;
        [SerializeField] private AssetReferenceSprite m_BackgroundImage;

        private Dictionary<AssetType, AddressablePath> m_Assets   = new();

        public string Id => name;
        public int Index => m_Index;

        public IReadOnlyList<IDialogueSpeaker> Speakers => m_Speakers;
        public IReadOnlyDictionary<AssetType, AddressablePath> Assets => m_Assets;

        public void Build(ActorSheet sheet)
        {
            foreach (var speaker in m_Speakers)
            {
                speaker.Build(sheet);
            }

            m_Assets[AssetType.BackgroundImage]
                = new AddressablePath($"{m_BackgroundImage.RuntimeKey}[{m_BackgroundImage.SubObjectName}]");
        }
    }

    [Serializable]
    public class DialogueSpeaker : IDialogueSpeaker
    {
        [SerializeField] private string m_Actor;
        [SerializeField] private string m_Message;
        [SerializeField] private float  m_Time;

        private IActorData actor;

        public IActorData Actor => actor;

        public string Message => m_Message;
        public float  Time    => m_Time;

        public void Build(ActorSheet sheet)
        {
            actor = sheet[m_Actor];
        }
    }
}