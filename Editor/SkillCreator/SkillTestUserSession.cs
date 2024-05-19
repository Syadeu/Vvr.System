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
// File created : 2024, 05, 19 20:05

#endregion

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session;
using Vvr.Session.Provider;

namespace Vvr.System.SkillCreator
{
    [UsedImplicitly]
    internal sealed class SkillTestUserSession : ChildSession<SkillTestUserSession.SessionData>,
        IUserActorProvider, IUserStageProvider,
        IConnector<IActorDataProvider>,
        IConnector<IStageDataProvider>,
        IConnector<ITestUserDataProvider>
    {
        public struct SessionData : ISessionData
        {
            public IEnumerable<string> playerActorIds;
            public IStageData          customStageData;
        }

        private IActorDataProvider    m_ActorDataProvider;
        private IStageDataProvider    m_StageDataProvider;
        private ITestUserDataProvider m_TestUserDataProvider;

        public override string DisplayName => nameof(UserSession);

        public IStageData CurrentStage => m_StageDataProvider.First().Value;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Parent.Register<IUserActorProvider>(this);
            Parent.Register<IUserStageProvider>(this);
            Vvr.Provider.Provider.Static.Connect<ITestUserDataProvider>(this);
            await base.OnInitialize(session, data);
        }
        protected override UniTask OnReserve()
        {
            Parent.Unregister<IUserActorProvider>();
            Parent.Unregister<IUserStageProvider>();
            Vvr.Provider.Provider.Static.Disconnect<ITestUserDataProvider>(this);
            return base.OnReserve();
        }

        public IReadOnlyList<IActorData> GetCurrentTeam()
        {
            return m_TestUserDataProvider.CurrentTeam.Select(m_ActorDataProvider.Resolve).ToArray();
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider    t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider    t) => m_StageDataProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageDataProvider = null;

        public void Connect(ITestUserDataProvider    t) => m_TestUserDataProvider = t;
        public void Disconnect(ITestUserDataProvider t) => m_TestUserDataProvider = null;
    }
}