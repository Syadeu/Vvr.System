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
// File created : 2024, 06, 30 19:06

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using JetBrains.Annotations;
using Unity.Collections;
using Vvr.Crypto;
using Vvr.Model.Wallet;
using Vvr.Provider;
using Vvr.Provider.Command;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    [UniqueSession]
    public sealed class UserWalletDataSession : ChildSession<UserWalletDataSession.SessionData>,
        IUserWalletProvider,
        IConnector<IWalletTypeProvider>,
        IConnector<IFirestoreProvider>,
        IConnector<IAuthenticationProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(UserWalletDataSession);

        private IFirestoreProvider      m_FirestoreProvider;
        private IAuthenticationProvider m_AuthenticationProvider;
        private IWalletTypeProvider     m_WalletTypeProvider;

        private readonly Dictionary<WalletType, CryptoFloat>
            m_Map = new();

        public float this[WalletType walletType]
        {
            get => m_Map.TryGetValue(walletType, out var f) ? f : 0;
        }

        public UniTask WaitForQueryFlush => UniTask.CompletedTask;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var walletSnapshot = await LoadWalletAsync();
            if (walletSnapshot.Exists)
            {
                foreach (var item in m_WalletTypeProvider)
                {
                    if (!walletSnapshot.TryGetValue(item.Value.Id, out float value))
                        continue;

                    m_Map[item.Key] = value;
                    $"Load wallet({item.Value.Id}): {value}".ToLog();
                }
            }
            else
            {
                $"No wallet data for {m_AuthenticationProvider.CurrentUserInfo.UserId}".ToLog();
            }
        }

        private async UniTask<DocumentSnapshot> LoadWalletAsync()
        {
            var docRef = m_FirestoreProvider.Instance
                .Collection(UserDataStructure.Users)
                .Document(m_AuthenticationProvider.CurrentUserInfo.UserId)
                .Collection(nameof(UserDataStructure.Private))
                .Document(UserDataStructure.Private.Wallet);

            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot;
        }

        public void Enqueue<TCommand>(TCommand command) where TCommand : IQueryCommand<UserWalletQuery>
        {
            using NativeStream st = new NativeStream(2, AllocatorManager.Temp);

            var query = new UserWalletQuery(st);
            {
                command.Execute(ref query);
            }
            query.Dispose();

            var rdr = st.AsReader();
            ProcessCommandQuery(ref rdr);
        }

        private void ProcessCommandQuery(ref NativeStream.Reader rdr)
        {
            int count = rdr.BeginForEachIndex(0);
            while (0 < count--)
            {
                UserWalletQuery.Entry e = rdr.Read<UserWalletQuery.Entry>();

                WalletType t = (WalletType)e.walletType;
                if (m_Map.TryGetValue(t, out var finalValue))
                    finalValue  += e.value;
                else finalValue =  e.value;

                m_Map[t] = finalValue;
            }

            rdr.EndForEachIndex();
        }

        public void Flush()
        {
            throw new System.NotImplementedException();
        }

        void IConnector<IWalletTypeProvider>.Connect(IWalletTypeProvider    t) => m_WalletTypeProvider = t;
        void IConnector<IWalletTypeProvider>.Disconnect(IWalletTypeProvider t) => m_WalletTypeProvider = null;

        void IConnector<IFirestoreProvider>.Connect(IFirestoreProvider    t) => m_FirestoreProvider = t;
        void IConnector<IFirestoreProvider>.Disconnect(IFirestoreProvider t) => m_FirestoreProvider = null;

        void IConnector<IAuthenticationProvider>.Connect(IAuthenticationProvider    t) => m_AuthenticationProvider = t;
        void IConnector<IAuthenticationProvider>.Disconnect(IAuthenticationProvider t) => m_AuthenticationProvider = null;
    }
}