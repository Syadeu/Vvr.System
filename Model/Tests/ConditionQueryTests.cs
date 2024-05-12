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
// File created : 2024, 05, 11 15:05
#endregion

using NUnit.Framework;

namespace Vvr.System.Model.Tests
{
    public class ConditionQueryTests
    {
        [Test]
        public void Test_1()
        {
            Condition p = Condition.OnHit;

            ConditionQuery query = p;
            Assert.AreEqual(1 ,query.Count);
            Assert.IsTrue(query.Has(p));
        }
        [Test]
        public void Test_2()
        {
            ConditionQuery query = Condition.GEqual;
            query |= Condition.OnHit;

            Assert.AreEqual(2, query.Count);
            Assert.IsTrue(query.Has(Condition.GEqual));
            Assert.IsTrue(query.Has(Condition.OnHit));
        }
        [Test]
        public void Test_3()
        {
            ConditionQuery query = Condition.GEqual;
            query |= Condition.OnHit;

            query &= Condition.GEqual;

            Assert.AreEqual(1, query.Count);
            Assert.IsTrue(query.Has(Condition.GEqual));
            Assert.IsFalse(query.Has(Condition.OnHit));
        }
    }
}