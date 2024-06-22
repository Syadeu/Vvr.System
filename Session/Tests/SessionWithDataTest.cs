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
// File created : 2024, 06, 22 14:06

#endregion

using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider.Command;
using Vvr.Session.AssetManagement;
using Vvr.TestClass;

namespace Vvr.Session.Tests
{
    [PublicAPI]
    public abstract class SessionWithDataTest<TRootSession> : SessionTest<TRootSession>
        where TRootSession : RootSession, IGameSessionBase, new()
    {
        public override async Task OneTimeSetUp()
        {
            await base.OneTimeSetUp();

            var results = await UniTask.WhenAll(
                Root.CreateSession<GameDataSession>(default),
                Root.CreateSession<CommandSession>(default)
            );

            Root.Register<ICommandProvider>(results.Item2);
        }
    }
}