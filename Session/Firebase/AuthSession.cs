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
using Firebase.Auth;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Provider;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    public sealed class AuthSession : ChildSession<AuthSession.SessionData>,
        IAuthenticationProvider
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(AuthSession);

        private FirebaseAuth m_Instance;

        public UserInfo CurrentUserInfo { get; private set; }

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            m_Instance = FirebaseAuth.DefaultInstance;

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_Instance = null;

            return base.OnReserve();
        }

        public async UniTask<UserInfo> SignInAnonymouslyAsync()
        {
            Assert.IsNull(CurrentUserInfo);

            AuthResult result = await m_Instance.SignInAnonymouslyAsync();
            $"{result.User.DisplayName}, {result.User.UserId}".ToLog();

            CurrentUserInfo = new UserInfo(result.User.DisplayName, result.User.UserId);

            return CurrentUserInfo;
        }
    }
}