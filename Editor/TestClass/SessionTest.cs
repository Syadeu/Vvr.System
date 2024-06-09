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
// File created : 2024, 06, 09 20:06

#endregion

using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Vvr.Provider;
using Vvr.Session;

namespace Vvr.TestClass
{
    public abstract class SessionTest<TRootSession>
        where TRootSession : RootSession, IGameSessionBase, new()
    {
        private CancellationTokenSource m_CancellationTokenSource;

        public TRootSession      Root              { get; private set; }
        public CancellationToken CancellationToken => m_CancellationTokenSource.Token;

        [OneTimeSetUp]
        public async void SetUp()
        {
            m_CancellationTokenSource = new();

            await InitializeRootSession();
        }

        private async UniTask InitializeRootSession()
        {
            Root = new TRootSession();
            await Root.Initialize(Owner.Issue, null, null);
        }

        [OneTimeTearDown]
        public async void TearDown()
        {
            m_CancellationTokenSource.Cancel();

            if (Root is not null)
                await Root.Reserve();
        }
    }
}