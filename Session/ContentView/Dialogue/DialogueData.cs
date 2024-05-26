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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.Dialogue
{
    [CreateAssetMenu(menuName = "Vvr/Create DialogueData", fileName = "DialogueData", order = 0)]
    public class DialogueData : ScriptableObject, IDialogueData
    {
        [SerializeField] private int    m_Index;
        [SerializeField] private AssetReferenceSprite m_BackgroundImage;

        [Space] [SerializeField] private RawDialogueAttribute[] m_Attributes;

        [Space] [SerializeField]
        private DialogueData m_NextDialogue;

        private IDialogueAttribute[] m_ResolvedAttributes;

        public string Id => name;
        public int Index => m_Index;

        public IReadOnlyList<IDialogueAttribute>              Attributes
        {
            get
            {
                m_ResolvedAttributes ??= m_Attributes.Select(x => x.Value).ToArray();
                return m_ResolvedAttributes;
            }
        }

        public AssetReferenceSprite BackgroundImage => m_BackgroundImage;

        public IDialogueData NextDialogue => m_NextDialogue;
    }
}