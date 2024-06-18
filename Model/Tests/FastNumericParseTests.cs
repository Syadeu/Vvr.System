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
// File created : 2024, 05, 25 11:05

#endregion

using NUnit.Framework;
using UnityEngine;

namespace Vvr.Model.Tests
{
    [TestFixture]
    public sealed class FastNumericParseTests
    {
        [Test]
        [TestCase("123", 123)]
        [TestCase("5461", 5461)]
        [TestCase("1", 1)]
        [TestCase("10", 10)]
        [TestCase("11", 11)]
        public void IntTest(string input, int expected)
        {
            int result = FastInt.Parse(input);

            Assert.AreEqual(expected, result);
        }

        [Test]
        [TestCase("123", 123)]
        [TestCase("5461", 5461)]
        [TestCase("1", 1)]
        [TestCase("10", 10)]
        [TestCase("11", 11)]
        public void CILIntTest(string input, int expected)
        {
            int result = int.Parse(input);

            Assert.AreEqual(expected, result);
        }

        [Test]
        [TestCase("123.123", 123.123f)]
        [TestCase("5461.1664", 5461.1664f)]
        [TestCase("1.0", 1)]
        [TestCase("10.001", 10.001f)]
        [TestCase("11.000000", 11)]
        public void FloatTest(string input, float expected)
        {
            float result = FastFloat.Parse(input);

            Assert.IsTrue(
                Mathf.Approximately(expected, result));
        }
        [Test]
        [TestCase("123.123", 123.123f)]
        [TestCase("5461.1664", 5461.1664f)]
        [TestCase("1.0", 1)]
        [TestCase("10.001", 10.001f)]
        [TestCase("11.000000", 11)]
        public void CILFloatTest(string input, float expected)
        {
            float result = float.Parse(input);

            Assert.IsTrue(
                Mathf.Approximately(expected, result));
        }

        [Test]
        [Repeat(100000)]
        [TestCase("1234236647.11623", 1234236647.11623f)]
        public void HeavyLoadTest(string input, float expected)
        {
            float result = FastFloat.Parse(input);

            Assert.IsTrue(
                Mathf.Approximately(expected, result));
        }
        [Test]
        [Repeat(100000)]
        [TestCase("1234236647.11623", 1234236647.11623f)]
        public void CILHeavyLoadTest(string input, float expected)
        {
            float result = float.Parse(input);

            Assert.IsTrue(
                Mathf.Approximately(expected, result));
        }
    }
}