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
// File created : 2024, 06, 22 17:06

#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.TestClass
{
    [Serializable]
    public class TestActorData : TestData, IActorData
    {
        [SerializeField] private TestStatValues m_StatValues;

        [Space] [SerializeField] private string[] m_PassiveIds;
        [SerializeField]         private string[] m_SkillIds;

        private PassiveSheet.Row[] m_PassiveData;
        private ISkillData[]       m_SkillData;

        public ActorSheet.ActorType            Type       { get; }
        public int                             Grade      { get; }
        public int                             Population { get; }
        public IRawStatValues                  Stats      => m_StatValues;
        public IReadOnlyList<PassiveSheet.Row> Passive    => m_PassiveData;
        public IReadOnlyList<ISkillData>       Skills     => m_SkillData;
        public Dictionary<AssetType, string>   Assets     { get; }

        public TestActorData(
            string                          id, int index,
            ActorSheet.ActorType            type,
            int                             grade,
            int                             population,
            TestStatValues                  stats,
            string[] passive,
            string[] skills
            )
            : base(id, index)
        {
            Type         = type;
            Grade        = grade;
            Population   = population;
            m_StatValues = stats;

            m_PassiveIds = passive;
            m_SkillIds  = skills;
        }

        public TestActorData(
            string               id, int index,
            ActorSheet.ActorType type,
            int                  grade,
            int                  population,
            TestStatValues       stats)
            : base(id, index)
        {
            Type         = type;
            Grade        = grade;
            Population   = population;
            m_StatValues = stats;
        }

        public override void Build(GameDataSheets sheets)
        {
            if (m_PassiveIds?.Length > 0)
            {
                m_PassiveData = new PassiveSheet.Row[m_PassiveIds.Length];
                for (int i = 0; i < m_PassiveData.Length; i++)
                {
                    m_PassiveData[i] = sheets.Passive[m_PassiveIds[i]];
                }
            }

            if (m_SkillIds?.Length > 0)
            {
                m_SkillData = new ISkillData[m_SkillIds.Length];
                for (int i = 0; i < m_SkillData.Length; i++)
                {
                    m_SkillData[i] = sheets.Skills[m_SkillIds[i]];
                }
            }
        }
    }
}