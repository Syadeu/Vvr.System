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
// File created : 2024, 06, 22 18:06

#endregion

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.TestClass
{
    [Serializable]
    public class TestStatData : IStatData
    {
        public static TestStatData CreateRandom()
        {
            Model.Stat.StatType t = (StatType)UnityEngine.Random.Range(0, 64);
            return new TestStatData(
                t, UnityEngine.Random.Range(short.MinValue, short.MaxValue), null);
        }

        [SerializeField] [LabelText("Raw StatType")]
        private long m_StatType;

        [ShowInInspector]
        [LabelText("StatType")]
        public StatType StatType
        {
            get => (StatType)m_StatType;
            set => m_StatType = (long)value;
        }

        [SerializeField] private float m_Value;

        [Space] [SerializeField] private AssetReferenceSprite m_IconAsset;

        public float Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public string Id           => VvrTypeHelper.Enum<StatType>.ToString(StatType);
        public int    Index        { get; set; }
        public object IconAssetKey => m_IconAsset;

        public TestStatData(StatType statType, float value, AssetReferenceSprite iconAsset)
        {
            StatType    = statType;
            Value       = value;
            m_IconAsset = iconAsset;
        }
    }
}