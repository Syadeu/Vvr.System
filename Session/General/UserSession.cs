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

namespace Vvr.Session
{
    // TODO: Temp
    [UsedImplicitly]
    public class UserSession : ChildSession<UserSession.SessionData>,
        IPlayerActorProvider,
        IConnector<IActorDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        class PlayerActorData : IActorData
        {
            public ActorSheet.Row Data { get; }

            public PlayerActorData(ActorSheet.Row d)
            {
                Data = d;
            }
        }

        private IActorDataProvider m_ActorDataProvider;

        // TODO: temp
        private PlayerActorData[] m_CurrentActors;

        public override string DisplayName => nameof(UserSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Parent.Register<IPlayerActorProvider>(this);

            await base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Parent.Unregister<IPlayerActorProvider>();

            return base.OnReserve();
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider t)
        {
            m_ActorDataProvider = t;

            // TODO: Test code
            m_CurrentActors = new PlayerActorData[5];
            List<ActorSheet.Row> chList = new List<ActorSheet.Row>(
                m_ActorDataProvider.Where(x => x.Id.StartsWith("CH")));
            int i = 0;
            while (i < m_CurrentActors.Length)
            {
                chList.Shuffle();
                for (; i < chList.Count && i < m_CurrentActors.Length; i++)
                {
                    m_CurrentActors[i] = new PlayerActorData(chList[i]);
                }
            }
        }

        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;

        // TODO : Temp code
        public IReadOnlyList<IActorData> GetCurrentTeam()
        {
            return m_CurrentActors;
        }
    }

    [LocalProvider]
    public interface IPlayerActorProvider : IProvider
    {
        [PublicAPI]
        IReadOnlyList<IActorData> GetCurrentTeam();
    }
}