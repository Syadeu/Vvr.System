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
    [UsedImplicitly]
    public class UserSession : ParentSession<UserSession.SessionData>,
        IUserActorProvider, IUserStageProvider,
        IConnector<IActorDataProvider>,
        IConnector<IStageDataProvider>
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
            Parent.Register<IUserDataProvider>(await CreateSession<UserDataSession>(default));
            Parent.Register<IUserActorProvider>(this);
            Parent.Register<IUserStageProvider>(this);

            await base.OnInitialize(session, data);
        }
        protected override UniTask OnReserve()
        {
            Parent.Unregister<IUserDataProvider>();
            Parent.Unregister<IUserActorProvider>();
            Parent.Unregister<IUserStageProvider>();

            return base.OnReserve();
        }

        public IReadOnlyList<IActorData> GetCurrentTeam()
        {
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


    }

    [UsedImplicitly]
    public sealed class UserDataSession : ChildSession<UserDataSession.SessionData>,
        IUserDataProvider
    {
        public struct SessionData : ISessionData
        {
        }

        // TODO: temp
        private readonly Dictionary<string, object> m_DataStore = new();

        public override string DisplayName => nameof(UserDataSession);

        public int GetInt(UserDataKey key, int defaultValue = 0)
        {
            if (!m_DataStore.TryGetValue(key.ToString(), out var v))
                return defaultValue;

            return (int)v;
        }

        public float GetFloat(UserDataKey key, float defaultValue = 0)
        {
            if (!m_DataStore.TryGetValue(key.ToString(), out var v))
                return defaultValue;

            return (float)v;
        }

        public string GetString(UserDataKey key, string defaultValue = null)
        {
            if (!m_DataStore.TryGetValue(key.ToString(), out var v))
                return defaultValue;

            return (string)v;
        }

        public void SetInt(UserDataKey key, int value)
        {
            m_DataStore[key.ToString()] = value;
        }

        public void SetFloat(UserDataKey key, float value)
        {
            m_DataStore[key.ToString()] = value;
        }

        public void SetString(UserDataKey key, string value)
        {
            m_DataStore[key.ToString()] = value;
        }
    }
}