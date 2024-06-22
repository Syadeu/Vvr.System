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
using JetBrains.Annotations;
using NUnit.Framework;
using Vvr.Provider;
using Vvr.Session;

namespace Vvr.TestClass
{
    [PublicAPI]
    public abstract class SessionTest<TRootSession>
        where TRootSession : RootSession, IGameSessionBase, new()
    {
        private CancellationTokenSource m_RootSessionTokenSource;
        private CancellationTokenSource m_CancellationTokenSource;

        public TRootSession      Root              { get; private set; }
        public CancellationToken CancellationToken => m_CancellationTokenSource.Token;

        [OneTimeSetUp]
        public virtual async Task OneTimeSetUp()
        {
            m_RootSessionTokenSource = new();

            await InitializeRootSession(m_RootSessionTokenSource.Token)
                    .SuppressCancellationThrow()
                ;
        }

        [OneTimeTearDown]
        public virtual async Task OneTimeTearDown()
        {
            m_RootSessionTokenSource.Cancel();
            m_RootSessionTokenSource.Dispose();

            if (Root is not null)
                await Root.Reserve();
        }

        [SetUp]
        public virtual async Task SetUp()
        {
            m_CancellationTokenSource = new();
        }
        [TearDown]
        public virtual async Task TearDown()
        {
            m_CancellationTokenSource.Cancel();
            m_CancellationTokenSource.Dispose();

            await Root.CloseAllSessions();
            Root.UnregisterAll();
            Root.DisconnectAllObservers();
        }

        private async UniTask InitializeRootSession(CancellationToken cancellationToken)
        {
            Root = new TRootSession();
            await Root.Initialize(Owner.Issue, null, null)
                    .AttachExternalCancellation(cancellationToken)
                ;

            await OnRootSessionCreated(Root)
                    .AttachExternalCancellation(cancellationToken)
                ;
        }

        protected virtual UniTask OnRootSessionCreated(TRootSession session) => UniTask.CompletedTask;
    }
}