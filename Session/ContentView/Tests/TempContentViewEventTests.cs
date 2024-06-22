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
// File created : 2024, 06, 22 13:06

#endregion

using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Tests
{
    [TestFixture]
    public sealed class TempContentViewEventTests : ContentViewEventHandlerTestBase
    {
        [Test]
        public async Task Test_0()
        {
            int executed = 0;

            using var c
                = EventHandler.Temp(TestContentViewEvent.Test0, (@event, ctx) =>
                {
                    executed++;
                    return UniTask.CompletedTask;
                });

            await EventHandler.ExecuteAsync(TestContentViewEvent.Test0);

            Assert.AreEqual(1, executed);
        }
        [Test]
        public async Task Test_1()
        {
            int executed = 0;

            using (var c
                   = EventHandler.Temp(TestContentViewEvent.Test0, (@event, ctx) =>
                   {
                       executed++;
                       return UniTask.CompletedTask;
                   }))
            {
            }

            await EventHandler.ExecuteAsync(TestContentViewEvent.Test0);

            Assert.AreEqual(0, executed);
        }
    }
}