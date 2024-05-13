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
// File created : 2024, 05, 09 23:05
#endregion

using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Vvr.Model.Stat.Tests
{
    public class StatValueTests
    {
        [Test]
        public void PlusOperatorTest_0()
        {
            StatValues
                x = StatValues.Create(StatType.HP),
                y = StatValues.Create(StatType.ARM);

            StatValues result = x + y;
            Assert.IsTrue(result.Types == (StatType.HP | StatType.ARM));
            Assert.IsTrue(result.Values.Count == 2);
        }
        [Test]
        public void PlusOperatorTest_1()
        {
            StatValues
                x = StatValues.Create(StatType.HP),
                y = StatValues.Create(StatType.ARM);

            x[StatType.HP]  = 10;
            y[StatType.ARM] = 90;

            StatValues result = x + y;
            Assert.IsTrue(Mathf.Approximately(10, result[StatType.HP]), $"{result[StatType.HP]}");
            Assert.IsTrue(Mathf.Approximately(90, result[StatType.ARM]), $"{result[StatType.ARM]}");
        }
        [Test]
        public void PlusOperatorTest_2()
        {
            StatValues
                x = StatValues.Create(StatType.HP  | StatType.ATT),
                y = StatValues.Create(StatType.ARM | StatType.HP | StatType.DEF | StatType.ATT);

            x[StatType.HP]  = 10;
            x[StatType.ATT] = 50;

            y[StatType.HP]  = 90;
            y[StatType.ATT] = 50;
            y[StatType.ARM] = 200;

            StatValues result = x + y;
            Assert.IsTrue(Mathf.Approximately(100, result[StatType.HP]), $"{result[StatType.HP]}");
            Assert.IsTrue(Mathf.Approximately(100, result[StatType.ATT]), $"{result[StatType.ATT]}");
            Assert.IsTrue(Mathf.Approximately(200, result[StatType.ARM]), $"{result[StatType.ARM]}");
            Assert.IsTrue(Mathf.Approximately(0, result[StatType.DEF]), $"{result[StatType.DEF]}");
        }
        [Test]
        public void PlusOperatorTest_3()
        {
            StatValues
                x = StatValues.Create(StatType.HP  | StatType.ATT),
                y = StatValues.Create(StatType.ARM | StatType.HP | StatType.DEF | StatType.ATT);

            x[StatType.HP]  = 10;
            x[StatType.ATT] = 50;

            y[StatType.HP]  = 90;
            y[StatType.ATT] = 50;
            y[StatType.ARM] = 200;

            StatValues result = x - y;
            Assert.IsTrue(Mathf.Approximately(-80, result[StatType.HP]), $"{result[StatType.HP]}");
            Assert.IsTrue(Mathf.Approximately(0, result[StatType.ATT]), $"{result[StatType.ATT]}");
            Assert.IsTrue(Mathf.Approximately(-200, result[StatType.ARM]), $"{result[StatType.ARM]}");
            Assert.IsTrue(Mathf.Approximately(0, result[StatType.DEF]), $"{result[StatType.DEF]}");
        }

        [Test]
        public void AndOperatorTest_1()
        {
            StatValues
                x = StatValues.Create(StatType.HP);

            x[StatType.HP]  = 10;

            StatValues result = x | StatType.HP;
            Assert.AreEqual(StatType.HP, result.Types);
            Assert.AreEqual(x.Values, result.Values);
        }
        [Test]
        public void AndOperatorTest_2()
        {
            StatValues
                x = StatValues.Create(StatType.HP);

            x[StatType.HP] = 10;

            StatValues result = x | StatType.ARM;

            Assert.AreEqual(StatType.HP | StatType.ARM, result.Types);
            Assert.AreEqual(2, result.Values.Count);

            Assert.IsTrue(Mathf.Approximately(10, result[StatType.HP]), $"{result[StatType.HP]}");
        }
        [Test]
        public void AndOperatorTest_3()
        {
            StatValues
                x = StatValues.Create(StatType.HP);

            x[StatType.HP] = 10;

            x |= StatType.ARM;
            x |= StatType.SPD;

            Assert.AreEqual(StatType.HP | StatType.ARM | StatType.SPD, x.Types);
            Assert.AreEqual(3, x.Values.Count);

            Assert.IsTrue(Mathf.Approximately(10, x[StatType.HP]), $"{x[StatType.HP]}");

            x[StatType.ARM] = 100;
            x[StatType.SPD] = 850;

            Assert.IsTrue(Mathf.Approximately(100, x[StatType.ARM]), $"{x[StatType.ARM]}");
            Assert.IsTrue(Mathf.Approximately(850, x[StatType.SPD]), $"{x[StatType.SPD]}");
        }

        [Test]
        public void UnknownTypeTest()
        {
            StatType unknownType1 = (StatType)(1L << 50);
            StatType unknownType2 = (StatType)(1L << 40);
            StatType unknownType3 = (StatType)(1L << 35);
            StatValues
                x = StatValues.Create(unknownType1 | unknownType2 | unknownType3);

            x[unknownType1] = 10;
            x[unknownType2] = 506;
            x[unknownType3] = 123124;

            Assert.IsTrue(Mathf.Approximately(10, x[unknownType1]),
                $"{x[unknownType1]}");
            Assert.IsTrue(Mathf.Approximately(506, x[unknownType2]),
                $"{x[unknownType2]}");
            Assert.IsTrue(Mathf.Approximately(123124, x[unknownType3]),
                $"{x[unknownType3]}");
        }

        [Test]
        public void JsonTest_1()
        {
            StatValues
                x = StatValues.Create(StatType.HP);

            x[StatType.HP] = 10;

            string     json         = JsonConvert.SerializeObject(x);
            StatValues deserialized = JsonConvert.DeserializeObject<StatValues>(json);
            Assert.AreEqual(x.Types, deserialized.Types);
            Assert.AreEqual(x.Values.Count, deserialized.Values.Count);

            Assert.IsTrue(Mathf.Approximately(10, deserialized[StatType.HP]), $"{deserialized[StatType.HP]}");
        }

        [Test]
        public void JsonTest_2()
        {
            StatType unknownType1 = (StatType)(1L << 50);
            StatType unknownType2 = (StatType)(1L << 40);
            StatType unknownType3 = (StatType)(1L << 35);
            StatValues
                x = StatValues.Create(unknownType1 | unknownType2 | unknownType3);

            x[unknownType1] = 10;
            x[unknownType2] = 506;
            x[unknownType3] = 123124;

            Debug.Log(x);

            string     json         = JsonConvert.SerializeObject(x);
            StatValues deserialized = JsonConvert.DeserializeObject<StatValues>(json);
            Assert.AreEqual(x.Types, deserialized.Types);
            Assert.AreEqual(x.Values.Count, deserialized.Values.Count);

            Assert.IsTrue(Mathf.Approximately(x[unknownType1], deserialized[unknownType1]), $"{deserialized[unknownType1]}");
            Assert.IsTrue(Mathf.Approximately(x[unknownType2], deserialized[unknownType2]), $"{deserialized[unknownType2]}");
            Assert.IsTrue(Mathf.Approximately(x[unknownType3], deserialized[unknownType3]), $"{deserialized[unknownType3]}");

            Debug.Log(deserialized);
        }
    }
}