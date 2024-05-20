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
// File created : 2024, 05, 21 00:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session;
using Vvr.Session.Provider;

namespace Vvr.TestClass
{
    [UsedImplicitly]
    public sealed class FakeUserSession : ChildSession<FakeUserSession.SessionData>,
        IUserActorProvider, IUserStageProvider,
        IConnector<IActorDataProvider>,
        IConnector<IStageDataProvider>
    {
        public struct SessionData : ISessionData
        {
            public IEnumerable<string> playerActorIds;
            public string              customStageId;
            public TestStageData          testStageData;

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

        private IActorDataProvider    m_ActorDataProvider;
        private IStageDataProvider    m_StageDataProvider;

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

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Parent.Register<IUserActorProvider>(this);
            Parent.Register<IUserStageProvider>(this);
            await base.OnInitialize(session, data);
        }
        protected override UniTask OnReserve()
        {
            Parent.Unregister<IUserActorProvider>();
            Parent.Unregister<IUserStageProvider>();
            return base.OnReserve();
        }

        private IActorData[] m_Actors;

        public IReadOnlyList<IActorData> GetCurrentTeam()
        {
            if (m_Actors == null)
            {
                if (!Data.playerActorIds.Any())
                {
                    m_Actors = Array.Empty<IActorData>();
                }
                else
                {
                    m_Actors = Data.playerActorIds
                        .Select(m_ActorDataProvider.Resolve)
                        .ToArray();
                }
            }

            return m_Actors;
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider    t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider    t) => m_StageDataProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageDataProvider = null;
    }
}