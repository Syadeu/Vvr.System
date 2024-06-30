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
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    public sealed class FirestoreSession : ChildSession<FirestoreSession.SessionData>,
        IFirestoreProvider,

        IConnector<IAuthenticationProvider>
    {
        public struct SessionData : ISessionData
        {
            public FirebaseApp app;
        }

        public override string DisplayName => nameof(FirestoreSession);

        private IAuthenticationProvider m_AuthenticationProvider;

        public FirebaseFirestore Instance { get; private set; }

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            Instance = FirebaseFirestore.DefaultInstance;
        }

        protected override UniTask OnReserve()
        {
            Instance = null;

            return base.OnReserve();
        }

        void IConnector<IAuthenticationProvider>.Connect(IAuthenticationProvider    t) => m_AuthenticationProvider = t;
        void IConnector<IAuthenticationProvider>.Disconnect(IAuthenticationProvider t) => m_AuthenticationProvider = null;
    }
}