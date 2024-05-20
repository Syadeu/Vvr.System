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
// File created : 2024, 05, 21 00:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Vvr.Model;

namespace Vvr.TestClass
{
    [Serializable]
    public class TestDialogueData : TestData, IDialogueData
    {
        [SerializeField] private TestDialogueSpeaker[] m_Speakers;

        [SerializeField] private AssetReferenceSprite m_BackgroundImage;

        private List<IDialogueSpeaker>                 m_ResolvedSpeakers;
        private Dictionary<AssetType, AddressablePath> m_Assets;

        public IReadOnlyList<IDialogueSpeaker>                 Speakers => m_ResolvedSpeakers;
        public IReadOnlyDictionary<AssetType, AddressablePath> Assets   => m_Assets;

        public override void Build(GameDataSheets sheets)
        {
            m_ResolvedSpeakers = new();
            foreach (var test in m_Speakers)
            {
                test.Build(sheets.Actors);

                m_ResolvedSpeakers.Add(test);
            }

            m_Assets                            = new();
            m_Assets[AssetType.BackgroundImage] = m_BackgroundImage.Resolve();
        }
    }
}