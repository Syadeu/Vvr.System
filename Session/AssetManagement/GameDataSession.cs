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
// File created : 2024, 05, 17 13:05

#endregion

using Cysharp.Threading.Tasks;
using Vvr.Provider;
using Vvr.Session.World;

namespace Vvr.Session
{
    public class GameDataSession : ParentSession<GameDataSession.SessionData>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(GameDataSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var assetSession = await CreateSession<AssetSession>(
                new AssetSession.SessionData(
                    "Game Data Group"
                    ));
            Register<IAssetProvider>(assetSession);
        }
    }
}