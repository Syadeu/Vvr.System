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

using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using JetBrains.Annotations;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    public sealed class FirestoreSession : ChildSession<FirestoreSession.SessionData>
    {
        struct UserStructure
        {
            public const string Users = nameof(Users);

            public struct Private
            {
                public const string Wallet = nameof(Wallet);
            }
        }

        public struct SessionData : ISessionData
        {
            public FirebaseApp app;
        }

        public override string DisplayName => nameof(FirestoreSession);

        private FirebaseFirestore m_Instance;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_Instance = FirebaseFirestore.DefaultInstance;
        }

        protected override UniTask OnReserve()
        {
            m_Instance = null;

            return base.OnReserve();
        }

        public async UniTask LoadWalletAsync()
        {
            const string TempId = "1234";

            var docRef = m_Instance
                .Collection(UserStructure.Users)
                .Document(TempId)
                .Collection(nameof(UserStructure.Private))
                .Document(UserStructure.Private.Wallet);

            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {

            }
        }
    }
}