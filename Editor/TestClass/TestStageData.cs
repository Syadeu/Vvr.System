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
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using Vvr.Model;

namespace Vvr.TestClass
{
    [Serializable]
    public class TestStageData : TestData, IStageData
    {
        [SerializeField] private string m_Name = "TESTSTAGE";
        [SerializeField] private int    m_Region;
        [SerializeField] private int    m_Floor;

        [SerializeField] private string[] m_Actors;

        [SerializeField] private AssetReferenceSprite m_BackgroundImage;

        private GameDataSheets   m_Sheets;
        private List<IActorData> m_ResolvedActors;

        private Dictionary<AssetType, AddressablePath> m_Assets;

        public string Name   => m_Name;
        public int    Region => m_Region;
        public int    Floor  => m_Floor;

        public IReadOnlyList<IActorData>                       Actors => m_ResolvedActors;
        public IReadOnlyDictionary<AssetType, AddressablePath> Assets => m_Assets;

        public override void Build(GameDataSheets data)
        {
            m_Sheets = data;

            m_Assets                            = new();
            m_Assets[AssetType.BackgroundImage] = m_BackgroundImage.Resolve();

            m_ResolvedActors = new();
            foreach (var actor in m_Actors)
            {
                m_ResolvedActors.Add(data.Actors[actor]);
            }
        }
        public TestStageData AddActor(IEnumerable<string> actors)
        {
            Assert.IsNotNull(m_Sheets);
            foreach (var actorId in actors)
            {
                m_ResolvedActors.Add(m_Sheets.Actors[actorId]);
            }

            return this;
        }
    }
}