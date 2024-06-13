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
// File created : 2024, 06, 13 21:06

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vvr.Crypto;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class UserActorDataSession : ChildSession<UserActorDataSession.SessionData>,
        IUserActorProvider,
        IConnector<IActorDataProvider>
    {
        sealed class ResolvedActorData : IResolvedActorData, IComparable<ResolvedActorData>
        {
            public static Func<ResolvedActorData, int> KeySelector { get; } = x => x.UniqueId;

            private readonly IActorData m_RawData;
            private readonly int        m_UniqueId;

            // TODO: provides low security
            private CryptoInt   m_Level;
            private CryptoFloat m_Exp;

            public int    Index => m_RawData.Index;
            public string Id    => m_RawData.Id;

            public int UniqueId => m_UniqueId;

            public ActorSheet.ActorType Type       => m_RawData.Type;
            public int                  Grade      => m_RawData.Grade;
            public int                  Population => m_RawData.Population;

            public IReadOnlyStatValues             Stats   => m_RawData.Stats;
            public IReadOnlyList<PassiveSheet.Row> Passive => m_RawData.Passive;
            public IReadOnlyList<SkillSheet.Row>   Skills  => m_RawData.Skills;
            public Dictionary<AssetType, string>   Assets  => m_RawData.Assets;

            public int Level
            {
                get => m_Level;
                set => m_Level = value;
            }
            public float Exp
            {
                get => m_Exp;
                set => m_Exp = value;
            }

            public ResolvedActorData(IActorData rawData, UserActorData data)
            {
                m_RawData  = rawData;
                m_UniqueId = data.uniqueId;

                m_Level = data.level ?? 0;
                m_Exp   = data.exp   ?? 0;
            }

            public UserActorData GetUserActorData()
            {
                return new UserActorData()
                {
                    uniqueId = UniqueId,
                    id       = Id,
                    level    = Level,
                    exp      = Exp
                };
            }

            public int CompareTo(ResolvedActorData other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return UniqueId.CompareTo(other.UniqueId);
            }
        }

        public struct SessionData : ISessionData
        {
            public IDataProvider dataProvider;
        }

        public override string DisplayName => nameof(UserActorDataSession);

        private IActorDataProvider m_ActorDataProvider;

        private readonly List<ResolvedActorData> m_ResolvedData = new();
        private          ResolvedActorData[]     m_CurrentTeam;

        private bool m_UserActorResolved;

        public IReadOnlyList<IResolvedActorData> PlayerActors => m_ResolvedData;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            SetupUserActors();

            return base.OnInitialize(session, data);
        }

        public IReadOnlyList<IResolvedActorData> GetCurrentTeam()
        {
            if (m_CurrentTeam is null)
            {
                m_CurrentTeam = new ResolvedActorData[5];

                var jr = Data.dataProvider.GetJson(UserDataKeyCollection.Actor.CurrentTeam());
                if (jr is not null)
                {
                    JArray arr = (JArray)jr;
                    for (int i = 0; i < arr.Count && i < m_CurrentTeam.Length; i++)
                    {
                        int uniqueId = arr[i].Value<int>();
                        if (uniqueId == 0) continue;

                        ResolvedActorData d = m_ResolvedData
                            .BinarySearch(ResolvedActorData.KeySelector, uniqueId);
                        m_CurrentTeam[i] = d;
                    }
                }
            }

            return m_CurrentTeam;
        }

        public void Flush()
        {
            JObject userActorArray = new();
            for (int i = 0; i < m_ResolvedData.Count; i++)
            {
                var e = m_ResolvedData[i];
                userActorArray.Add(
                    e.UniqueId.ToString(),
                    JObject.FromObject(e.GetUserActorData()));
            }

            JArray currentTeamArray = new();
            for (int i = 0; i < m_CurrentTeam.Length; i++)
            {
                currentTeamArray.Add(m_CurrentTeam[i].UniqueId);
            }

            Data.dataProvider
                .SetJson(UserDataKeyCollection.Actor.UserActors(), userActorArray);
            Data.dataProvider
                .SetJson(UserDataKeyCollection.Actor.CurrentTeam(), currentTeamArray);
        }

        private void SetupUserActors()
        {
            if (m_UserActorResolved) return;

            var jr = Data.dataProvider.GetJson(UserDataKeyCollection.Actor.UserActors());
            if (jr is not null)
            {
                foreach (var item in (JObject)jr)
                {
                    UserActorData d       = item.Value.ToObject<UserActorData>();
                    IActorData    rawData = m_ActorDataProvider.Resolve(d.id);

                    var data = new ResolvedActorData(rawData, d);
                    m_ResolvedData.AddWithOrder(data);
                }
            }

            m_UserActorResolved = true;
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider    t)
        {
            m_ActorDataProvider = t;

            if (Initialized)
                SetupUserActors();
        }

        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;
    }
}