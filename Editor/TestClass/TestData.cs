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
using UnityEngine;
using Vvr.Model;

namespace Vvr.TestClass
{
    [Serializable]
    public abstract class TestData : IRawData
    {
        [SerializeField] private string m_Id = "TEST";
        [SerializeField] private int    m_Index;

        public string Id    => m_Id;
        public int    Index => m_Index;

        protected TestData(string id, int index)
        {
            m_Id    = id;
            m_Index = index;
        }

        public abstract void Build(GameDataSheets sheets);
    }
}