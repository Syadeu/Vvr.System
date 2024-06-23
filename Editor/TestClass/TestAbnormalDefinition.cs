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
// File created : 2024, 06, 23 01:06

#endregion

using System;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Model;

namespace Vvr.TestClass
{
    [PublicAPI]
    [Serializable]
    public class TestAbnormalDefinition : IAbnormalDefinition
    {
        [SerializeField]
        private int       m_Type;
        [SerializeField]
        private int       m_Level;
        [SerializeField]
        private bool      m_IsBuff;
        [SerializeField]
        private bool      m_Replaceable;
        [SerializeField]
        private int       m_MaxStack;
        [SerializeField]
        private Method    m_Method;
        [SerializeField]
        private TestStatData m_TargetStatus;
        [SerializeField]
        private float     m_Value;

        public int Type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public int Level
        {
            get => m_Level;
            set => m_Level = value;
        }

        public bool IsBuff
        {
            get => m_IsBuff;
            set => m_IsBuff = value;
        }

        public bool Replaceable
        {
            get => m_Replaceable;
            set => m_Replaceable = value;
        }

        public int MaxStack
        {
            get => m_MaxStack;
            set => m_MaxStack = value;
        }

        public Method Method
        {
            get => m_Method;
            set => m_Method = value;
        }

        IStatData IAbnormalDefinition.TargetStatus => m_TargetStatus;

        public TestStatData TargetStatus
        {
            get => m_TargetStatus;
            set => m_TargetStatus = value;
        }

        public float Value
        {
            get => m_Value;
            set => m_Value = value;
        }
    }

    [Serializable]
    public class TestAbnormalDuration : IAbnormalDuration
    {
        [SerializeField]
        private float m_DelayTime;
        [SerializeField]
        private float m_Time;

        public float DelayTime
        {
            get => m_DelayTime;
            set => m_DelayTime = value;
        }

        public float Time
        {
            get => m_Time;
            set => m_Time = value;
        }
    }

    [PublicAPI]
    [Serializable]
    public class TestAbnormalUpdate : IAbnormalUpdate
    {
        [SerializeField]
        private bool      m_Enable;
        [SerializeField]
        private Condition m_Condition;
        [SerializeField]
        private string    m_Value;
        [SerializeField]
        private float     m_Interval;
        [SerializeField]
        private int       m_MaxCount;

        public bool Enable
        {
            get => m_Enable;
            set => m_Enable = value;
        }

        public Condition Condition
        {
            get => m_Condition;
            set => m_Condition = value;
        }

        public string Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public float Interval
        {
            get => m_Interval;
            set => m_Interval = value;
        }

        public int MaxCount
        {
            get => m_MaxCount;
            set => m_MaxCount = value;
        }
    }

    [PublicAPI]
    [Serializable]
    public class TestAbnormalCancellation : IAbnormalCancellation
    {
        [SerializeField]
        private Condition m_Condition;
        [SerializeField]
        private float     m_Probability;
        [SerializeField]
        private string    m_Value;
        [SerializeField]
        private bool      m_ClearAllStacks;

        public Condition Condition
        {
            get => m_Condition;
            set => m_Condition = value;
        }

        public float Probability
        {
            get => m_Probability;
            set => m_Probability = value;
        }

        public string Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public bool ClearAllStacks
        {
            get => m_ClearAllStacks;
            set => m_ClearAllStacks = value;
        }
    }
}