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
// File created : 2024, 06, 30 20:06

#endregion

using System.Threading.Tasks;
using NUnit.Framework;
using Vvr.Provider;
using Vvr.Session.Tests;
using Vvr.TestClass;

namespace Vvr.Session.Firebase.Tests
{
    [TestFixture]
    public sealed class WalletDataTests : SessionWithDataTest<TestRootSession>
    {
        protected IAuthenticationProvider Authentication { get; set; }

        public override async Task OneTimeSetUp()
        {
            await base.OneTimeSetUp();

            Authentication = await Root.CreateSession<AuthSession>(default);
            await Authentication.SignInAnonymouslyAsync();
        }

        public override async Task OneTimeTearDown()
        {
            await Authentication.DeleteAccountAsync();
            await base.OneTimeTearDown();
        }
    }
}