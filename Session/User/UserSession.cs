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
// File created : 2024, 05, 17 00:05

#endregion

using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.User
{
    [UsedImplicitly]
    [ProviderSession(
        typeof(IUserDataProvider),
        typeof(IPlayerPrefDataProvider),
        typeof(IUserStageProvider),
        typeof(IUserActorProvider)
        )]
    public class UserSession : ParentSession<UserSession.SessionData>,
        IUserStageProvider,
        IConnector<IStageDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        private IStageDataProvider m_StageDataProvider;

        public override string DisplayName => nameof(UserSession);

        public IStageData CurrentStage => m_StageDataProvider.First().Value;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            PlayerPrefDataSession
                prefDataSession = await CreateSession<PlayerPrefDataSession>(default);

            Parent
                // IDataProvider Sessions
                .Register<IUserDataProvider>(await CreateSession<UserDataSession>(default))
                .Register<IPlayerPrefDataProvider>(prefDataSession)
                // IDataProvider

                .Register<IUserStageProvider>(this)
                .Register<IUserActorProvider>(await CreateSessionOnBackground<UserActorDataSession>(
                    new UserActorDataSession.SessionData()
                    {
                        // TODO: this is temporarily. Should changed to firebase
                        dataProvider = prefDataSession
                    }))
                .Register<IUserWalletProvider>(await CreateSessionOnBackground<UserWalletDataSession>(
                    new UserWalletDataSession.SessionData()
                    {
                        // TODO: This is temporarily
                        dataProvider = prefDataSession
                    }))
                ;

            await base.OnInitialize(session, data);
        }
        protected override UniTask OnReserve()
        {
            Parent
                .Unregister<IUserDataProvider>()
                .Unregister<IPlayerPrefDataProvider>()
                .Unregister<IUserActorProvider>()
                .Unregister<IUserStageProvider>();

            return base.OnReserve();
        }

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider    t) => m_StageDataProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageDataProvider = null;
    }
}