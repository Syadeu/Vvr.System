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

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    // TODO: Temp
    [UsedImplicitly]
    public class UserSession : ChildSession<UserSession.SessionData>,
        IUserActorProvider, IUserStageProvider,
        IConnector<IActorDataProvider>,
        IConnector<IStageDataProvider>

#if UNITY_EDITOR
        , IConnector<ITestUserDataProvider>
#endif
    {
        public struct SessionData : ISessionData
        {
        }

        private IActorDataProvider m_ActorDataProvider;
        private IStageDataProvider m_StageDataProvider;

        // TODO: temp
        private IActorData[] m_CurrentActors;

        public override string DisplayName => nameof(UserSession);

        public IStageData CurrentStage => m_StageDataProvider.First().Value;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Parent.Register<IUserActorProvider>(this);
            Parent.Register<IUserStageProvider>(this);

#if UNITY_EDITOR
            Vvr.Provider.Provider.Static.Connect<ITestUserDataProvider>(this);
#endif
            await base.OnInitialize(session, data);
        }
        protected override UniTask OnReserve()
        {
            Parent.Unregister<IUserActorProvider>();
            Parent.Unregister<IUserStageProvider>();

#if UNITY_EDITOR
            Vvr.Provider.Provider.Static.Disconnect<ITestUserDataProvider>(this);
#endif
            return base.OnReserve();
        }

        public IReadOnlyList<IActorData> GetCurrentTeam()
        {
            // TODO : Temp code
#if UNITY_EDITOR
            if (m_TestUserDataProvider != null)
            {
                return m_TestUserDataProvider.CurrentTeam.Select(m_ActorDataProvider.Resolve).ToArray();
            }
#endif

            return m_CurrentActors;
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider t)
        {
            m_ActorDataProvider = t;

            // TODO: Test code
            m_CurrentActors = new IActorData[5];
            List<IActorData> chList = new List<IActorData>(
                m_ActorDataProvider.Where(x => x.Id.StartsWith("CH")));
            int i = 0;
            while (i < m_CurrentActors.Length)
            {
                for (; i < chList.Count && i < m_CurrentActors.Length; i++)
                {
                    m_CurrentActors[i] = chList[i];
                }
                chList.Shuffle();
            }
        }
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;

        void IConnector<IStageDataProvider>.Connect(IStageDataProvider    t) => m_StageDataProvider = t;
        void IConnector<IStageDataProvider>.Disconnect(IStageDataProvider t) => m_StageDataProvider = null;

#if UNITY_EDITOR
        private ITestUserDataProvider m_TestUserDataProvider;
        public  void                  Connect(ITestUserDataProvider    t) => m_TestUserDataProvider = t;
        public  void                  Disconnect(ITestUserDataProvider t) => m_TestUserDataProvider = null;
#endif
    }
}