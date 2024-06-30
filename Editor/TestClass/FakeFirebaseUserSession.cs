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
// File created : 2024, 06, 30 17:06

#endregion

#if VVR_FIREBASE

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session;
using Vvr.Session.Firebase;
using Vvr.Session.User;

namespace Vvr.TestClass
{
    [UsedImplicitly]
    [ProviderSession(
        typeof(IAuthenticationProvider),
        typeof(IUserDataProvider),
        typeof(IPlayerPrefDataProvider),

        typeof(IUserStageProvider),
        typeof(IUserActorProvider)
        )]
    public sealed class FakeFirebaseUserSession : ParentSession<FakeFirebaseUserSession.SessionData>,
        IUserStageProvider,
        IConnector<IActorDataProvider>,
        IConnector<IStageDataProvider>
    {
        public struct SessionData : ISessionData
        {
            public IEnumerable<string> playerActorIds;
            public string              customStageId;
            public TestStageData       testStageData;

            public SessionData(IEnumerable<string> actors, string stageId)
            {
                playerActorIds = actors;
                customStageId  = stageId;
                testStageData  = null;
            }
            public SessionData(IEnumerable<string> actors, TestStageData stage)
            {
                playerActorIds = actors;
                testStageData  = stage;
                customStageId  = null;
            }
        }

        private IActorDataProvider m_ActorDataProvider;
        private IStageDataProvider m_StageDataProvider;

        private IActorData[] m_Actors;

        private readonly List<IActorData> m_UserActors = new();

        public override string DisplayName => nameof(UserSession);

        public IStageData CurrentStage
        {
            get
            {
                if (Data.customStageId.IsNullOrEmpty())
                {
                    return Data.testStageData;
                }

                return m_StageDataProvider[Data.customStageId];
            }
        }

        public IReadOnlyList<IActorData> PlayerActors => m_UserActors;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            IAuthenticationProvider auth = await CreateSession<AuthSession>(default);
            await auth.SignInAnonymouslyAsync();

            UserDataSession
                userDataSession = await CreateSession<UserDataSession>(default);
            PlayerPrefDataSession
                prefDataSession = await CreateSession<PlayerPrefDataSession>(default);

            Parent
                .Register<IUserDataProvider>(userDataSession)
                .Register<IPlayerPrefDataProvider>(prefDataSession)
                .Register<IUserStageProvider>(this);

            SetupFakeUserData(userDataSession);

            Parent
                .Register<IUserActorProvider>(await CreateSession<UserActorDataSession>(
                    new UserActorDataSession.SessionData()
                    {
                        dataProvider = userDataSession
                    }));

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

        private void SetupFakeUserData(IDataProvider dataProvider)
        {
            int i = 0;

            JObject userActorData    = new ();
            JArray  currentTeamArray = new();
            foreach (var actorId in Data.playerActorIds)
            {
                UserActorData d = new UserActorData(actorId);
                userActorData.Add(d.uniqueId.ToString(), JObject.FromObject(d));

                if (i++ < 5)
                    currentTeamArray.Add(d.uniqueId);
            }

            dataProvider.SetJson(UserDataKeyCollection.Actor.UserActors(), userActorData);
            dataProvider.SetJson(UserDataKeyCollection.Actor.CurrentTeam(), currentTeamArray);
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider    t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider    t) => m_StageDataProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageDataProvider = null;
    }
}

#endif