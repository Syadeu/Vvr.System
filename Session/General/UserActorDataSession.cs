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
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Crypto;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.Provider.Command;
using Vvr.Session.AssetManagement;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class UserActorDataSession : ParentSession<UserActorDataSession.SessionData>,
        IUserActorProvider,
        IConnector<IActorDataProvider>
    {
        public const int TEAM_COUNT = 3;

        sealed class ResolvedActorData : IResolvedActorData, IComparable<ResolvedActorData>
        {
            public static Func<ResolvedActorData, int> KeySelector { get; } = x => x.UniqueId;

            private readonly IAssetProvider m_AssetProvider;
            private readonly IActorData     m_RawData;

            // TODO: provides low security
            private CryptoInt   m_Level;
            private CryptoFloat m_Exp;

            public int    Index => m_RawData.Index;
            public string Id    => m_RawData.Id;

            public int UniqueId { get; }

            public ActorSheet.ActorType Type       => m_RawData.Type;
            public int                  Grade      => m_RawData.Grade;
            public int                  Population => m_RawData.Population;

            public IRawStatValues                  Stats   => m_RawData.Stats;
            public IReadOnlyList<PassiveSheet.Row> Passive => m_RawData.Passive;
            public IReadOnlyList<ISkillData>       Skills  => m_RawData.Skills;
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

            public ResolvedActorData(
                IAssetProvider assetProvider,
                IActorData rawData, UserActorData data)
            {
                m_AssetProvider = assetProvider;
                m_RawData       = rawData;
                UniqueId        = data.uniqueId;

                m_Level = data.level ?? 0;
                m_Exp   = data.exp   ?? 0;
            }

            public UniTask<IImmutableObject<Sprite>> LoadContextPortrait()
            {
                return m_AssetProvider.LoadAsync<Sprite>(Assets[AssetType.ContextPortrait]);
            }

            public UniTask<IImmutableObject<Sprite>> LoadSkillIcon(int index)
            {
                return m_AssetProvider.LoadAsync<Sprite>(Skills[index].IconAssetKey);
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

        private IAssetProvider     m_AssetProvider;
        private IActorDataProvider m_ActorDataProvider;

        // This list is ordered list by unique id
        private readonly List<ResolvedActorData> m_ResolvedData = new();
        // This array length must TEAM_COUNT
        private          ResolvedActorData[]     m_CurrentTeam;

        private bool m_UserActorResolved;

        public IReadOnlyList<IResolvedActorData> PlayerActors => m_ResolvedData;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSessionOnBackground<AssetSession>(default);

            SetupUserActors();
        }

        UniTask ICommandProvider.EnqueueAsync<TCommand>(IEventTarget target)
        {
            TCommand cmd = Activator.CreateInstance<TCommand>();
            return ((ICommandProvider)this).EnqueueAsync(target, cmd);
        }
        async UniTask ICommandProvider.EnqueueAsync(IEventTarget target, ICommand command)
        {
            if (command is not IUserDataCommand)
                throw new InvalidOperationException("Command excepts only " + nameof(IUserDataCommand));

            this.Inject(command);
            await command.ExecuteAsync(target);
        }

        public void Enqueue<TCommand>(TCommand command) where TCommand : IQueryCommand<UserActorDataQuery>
        {
            // TODO: cache the steam when multiple call
            using NativeStream st = new NativeStream(4, AllocatorManager.Temp);

            var query = new UserActorDataQuery(st);
            {
                command.Execute(ref query);
            }

            // TODO: Should separate execution?

            var rdr   = st.AsReader();
            int count = rdr.BeginForEachIndex(0);

            for (int i = 0; i < count; i++)
            {
                UserActorDataQuery.CommandType t = (UserActorDataQuery.CommandType)rdr.Read<short>();

                switch (t)
                {
                    case UserActorDataQuery.CommandType.SetTeamActor:
                        ProcessCommandData(rdr.Read<UserActorDataQuery.SetTeamActorData>());
                        i++;
                        break;
                    case UserActorDataQuery.CommandType.Flush:
                        Flush();
                        break;
                    case UserActorDataQuery.CommandType.ResetData:
                        LoadCurrentTeam();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid command type: " + t.ToString());
                }
            }

            rdr.EndForEachIndex();
        }

        private void ProcessCommandData(UserActorDataQuery.SetTeamActorData data)
        {
            if (data.index is < 0 or >= TEAM_COUNT)
                throw new InvalidOperationException($"{data.index}");

            ResolvedActorData targetData
                = m_ResolvedData.BinarySearch(ResolvedActorData.KeySelector, data.id);
            Assert.IsNotNull(targetData);

            // If target actor is in team
            if (TryGetCurrentTeamIndex(targetData, out int targetTeamIndex))
            {
                // If trying to insert at same index
                if (targetTeamIndex == data.index)
                    // XXX: need to consideration that this operation might unintentional.
                    return;

                // Can be swap if both actors in the same team
                m_CurrentTeam[targetTeamIndex] = m_CurrentTeam[data.index];
            }
            m_CurrentTeam[data.index] = targetData;
        }

        private bool TryGetCurrentTeamIndex(ResolvedActorData d, out int index)
        {
            for (int i = 0; i < m_CurrentTeam.Length; i++)
            {
                var e = m_CurrentTeam[i];
                if (e == d)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
        public IReadOnlyList<IResolvedActorData> GetCurrentTeam()
        {
            if (m_CurrentTeam is null)
            {
                LoadCurrentTeam();
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

        private void LoadCurrentTeam()
        {
            m_CurrentTeam ??= new ResolvedActorData[TEAM_COUNT];

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
        private void SetupUserActors()
        {
            if (m_UserActorResolved) return;

            Assert.IsNotNull(m_AssetProvider);

            var jr = Data.dataProvider.GetJson(UserDataKeyCollection.Actor.UserActors());
            if (jr is not null)
            {
                foreach (var item in (JObject)jr)
                {
                    UserActorData d       = item.Value.ToObject<UserActorData>();
                    IActorData    rawData = m_ActorDataProvider.Resolve(d.id);

                    var data = new ResolvedActorData(m_AssetProvider, rawData, d);
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