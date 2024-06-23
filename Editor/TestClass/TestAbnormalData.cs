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
// File created : 2024, 06, 23 03:06

#endregion

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Model;

namespace Vvr.TestClass
{
    [PublicAPI]
    [Serializable]
    public class TestAbnormalData : IAbnormalData
    {
        [SerializeField] private string                   m_Id;
        [SerializeField] private int                      m_Index;
        [SerializeField] private TestAbnormalDefinition   m_Definition;
        [SerializeField] private TestAbnormalDuration     m_Duration;
        [SerializeField] private List<Condition>          m_TimeCondition;
        [SerializeField] private TestAbnormalUpdate       m_Update;
        [SerializeField] private TestAbnormalCancellation m_Cancellation;

        private readonly List<IAbnormalData> m_AbnormalChain = new();

        public string Id
        {
            get => m_Id;
            set => m_Id = value;
        }
        public int Index
        {
            get => m_Index;
            set => m_Index = value;
        }

        IAbnormalDefinition IAbnormalData.         Definition    => m_Definition;
        IAbnormalDuration IAbnormalData.           Duration      => m_Duration;
        IReadOnlyList<Condition> IAbnormalData.    TimeCondition => m_TimeCondition;
        IAbnormalUpdate IAbnormalData.             Update        => m_Update;
        IAbnormalCancellation IAbnormalData.       Cancellation  => m_Cancellation;
        IReadOnlyList<IAbnormalData> IAbnormalData.AbnormalChain => m_AbnormalChain;

        public IList<IAbnormalData> AbnormalChain => m_AbnormalChain;
    }
}