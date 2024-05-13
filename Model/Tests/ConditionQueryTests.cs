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

using System;
using NUnit.Framework;

namespace Vvr.Model.Tests
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
        [Test]
        public void Test_4()
        {
            ConditionQuery query = Condition.GEqual;
            query |= Condition.OnHit;
            query |= Condition.HasAbnormal;
            query |= Condition.HasPassive;
            query |= Condition.IsInHand;
            query |= Condition.IsPlayerActor;

            ConditionQuery targetQuery = Condition.HasPassive;
            targetQuery |= Condition.HasAbnormal;
            targetQuery |= Condition.OnActorDead;

            query &= targetQuery;

            Assert.AreEqual(2, query.Count);
            Assert.IsTrue(query.Has(Condition.HasPassive));
            Assert.IsTrue(query.Has(Condition.HasAbnormal));

            Assert.IsFalse(query.Has(Condition.GEqual));
            Assert.IsFalse(query.Has(Condition.OnHit));
            Assert.IsFalse(query.Has(Condition.OnActorDead));
            Assert.IsFalse(query.Has(Condition.IsInHand));
            Assert.IsFalse(query.Has(Condition.IsPlayerActor));
        }
        [Test]
        public void Test_5()
        {
            ConditionQuery query = (Condition)0;
            query |= (Condition)1;
            query |= (Condition)2;
            query |= (Condition)5;
            query |= (Condition)45;
            query |= (Condition)63;

            ConditionQuery targetQuery = (Condition)80;
            targetQuery |= (Condition)45;
            targetQuery |= (Condition)63;
            targetQuery |= (Condition)100;

            query &= targetQuery;

            Assert.AreEqual(2, query.Count);
            Assert.IsTrue(query.Has((Condition)45));
            Assert.IsTrue(query.Has((Condition)63));
        }
        [Test]
        public void Test_6()
        {
            ConditionQuery query = (Condition)0;
            query |= (Condition)1;
            query |= (Condition)2;
            query |= (Condition)5;
            query |= (Condition)45;

            Assert.Catch<InvalidOperationException>(() => query |= (Condition)100);

            ConditionQuery targetQuery = (Condition)80;
            targetQuery |= (Condition)45;
            targetQuery |= (Condition)63;
            targetQuery |= (Condition)100;

            Assert.Catch<InvalidOperationException>(() => query |= targetQuery);
        }
        [Test]
        public void Test_7()
        {
            ConditionQuery query = (Condition)0;
            query |= (Condition)1;
            query |= (Condition)2;
            query |= (Condition)5;
            query |= (Condition)63;

            ConditionQuery targetQuery = (Condition)123;
            targetQuery |= (Condition)63;
            targetQuery |= (Condition)120;
            targetQuery |= (Condition)122;

            ConditionQuery result = query & targetQuery;

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual((Condition)63, result.Last);
            Assert.IsTrue(result.Has((Condition)63));
        }
    }
}