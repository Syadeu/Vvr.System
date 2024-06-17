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
// File created : 2024, 06, 17 21:06
#endregion

using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using Vvr.Model.Stat;
using Vvr.TestClass;

namespace Vvr.Controller.Stat.Tests
{
    [PublicAPI]
    public class StatValueStackTests
    {
        #region Initialize

        private TestActor m_TestActor;

        private StatValues     m_OriginalStats;
        private StatValueStack m_StatValueStack;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestActor = new TestActor(OwnerHelper.Player, "Test Actor", null);
        }
        [SetUp]
        public void SetUp()
        {
            m_OriginalStats   = GetOriginalStatValues();
            m_StatValueStack  = new StatValueStack(m_TestActor, m_OriginalStats);
            m_TestActor.Stats = m_StatValueStack;
        }

        [TearDown]
        public void TearDown()
        {
            m_TestActor.Stats = null;
            m_StatValueStack.Dispose();
        }

        protected virtual StatValues GetOriginalStatValues()
        {
            StatValues v = StatValues.Create(StatType.HP | StatType.ARM);
            v[StatType.HP]  = 10;
            v[StatType.ARM] = 5;

            return v;
        }

        #endregion

        protected TestActor      Actor          => m_TestActor;
        protected StatValues     OriginalStats  => m_OriginalStats;
        protected StatValueStack StatValueStack => m_StatValueStack;

        protected void AreSame(StatType type, float v)
        {
            Assert.IsTrue(
                Mathf.Approximately(StatValueStack[type], v));
        }

        [Test]
        public void PushTest_0()
        {
            StatValueStack.Push(StatType.HP, 10);

            AreSame(StatType.HP, OriginalStats[StatType.HP] + 10);
        }
        [Test]
        public void PushTest_1()
        {
            DamageProcessor processor = new DamageProcessor();

            StatValueStack.Push(StatType.HP, 10);

            AreSame(StatType.HP, OriginalStats[StatType.HP] + 10);
        }
    }
}