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
// File created : 2024, 06, 18 10:06

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Vvr.Model.Tests
{
    [TestFixture]
    public sealed class SemaphoreSlimLockTests
    {
        private SemaphoreSlim m_Lock;

        [SetUp]
        public void SetUp()
        {
            m_Lock = new(1, 1);
        }
        [TearDown]
        public void TearDown()
        {
            m_Lock.Dispose();
        }

        private void HeavyLoad()
        {
            using var l = new SemaphoreSlimLock(m_Lock);
            l.Wait(TimeSpan.FromSeconds(1));

            double p;
            for (int i = 0; i < 1000; i++)
            {
                p = Mathf.PI;
                p = p / Math.Sqrt(p);
            }
        }
        private async Task HeavyLoadTask()
        {
            using var l = new SemaphoreSlimLock(m_Lock);
            await l.WaitAsync(TimeSpan.FromSeconds(1));

            double p;
            for (int i = 0; i < 1000; i++)
            {
                p = Mathf.PI;
                p = p / Math.Sqrt(p);
            }
        }

        [Test]
        public void BasicSyncTest()
        {
            using (var l = new SemaphoreSlimLock(m_Lock))
            {
                l.Wait(TimeSpan.FromSeconds(1));
            }

            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }

        [Test]
        public async Task BasicAsyncTest_0()
        {
            var t0 = Task.Run(HeavyLoad);
            HeavyLoad();
            await t0;

            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }
        [Test]
        public async Task BasicAsyncTest_1()
        {
            await Task.WhenAll(
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad)
            );

            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }
        [Test]
        public async Task BasicAsyncTest_2()
        {
            await Task.WhenAll(
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad),
                Task.Run(HeavyLoad)
            );

            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }

        [Test]
        public async Task YieldTest_0()
        {
            await Task.WhenAll(
                HeavyLoadTask(),
                HeavyLoadTask()
                );
            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }
        [Test]
        public async Task YieldTest_1()
        {
            await Task.WhenAll(
                HeavyLoadTask(),
                Task.Run(HeavyLoadTask)
                );
            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }
        [Test]
        public async Task YieldTest_2()
        {
            await Task.WhenAll(
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask),
                Task.Run(HeavyLoadTask)
                );
            Assert.IsTrue(m_Lock.CurrentCount != 0);
        }
    }
}