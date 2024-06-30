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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    [UniqueSession, ProviderSession(
        typeof(IAuthenticationProvider)
        )]
    public sealed class AuthSession : ChildSession<AuthSession.SessionData>,
        IAuthenticationProvider
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(AuthSession);

        private FirebaseAuth m_Instance;

        private FirebaseUser m_CurrentUser;
        private Credential   m_CurrentCredential;

        private readonly List<IAuthenticationCallbacks> m_CallbacksList = new();

        [NotNull]
        public UserInfo CurrentUserInfo
        {
            get
            {
                if (m_CurrentUser is null)
                {
                    throw new InvalidOperationException(
                        "No login info"
                        );
                }

                return new UserInfo(m_CurrentUser.DisplayName, m_CurrentUser.UserId);
            }
        }
        public bool LoggedIn => m_CurrentUser is not null;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);
            await UniTask.SwitchToMainThread();

            m_Instance = FirebaseAuth.DefaultInstance;

            Parent.Register<IAuthenticationProvider>(this);
        }

        protected override UniTask OnReserve()
        {
            Parent.Unregister<IAuthenticationProvider>();

            m_Instance = null;

            return base.OnReserve();
        }

        public async UniTask<UserInfo> SignInAnonymouslyAsync()
        {
            // https://github.com/firebase/quickstart-unity/issues/1320

            if (m_CurrentUser is not null)
            {
                return new UserInfo(m_CurrentUser.DisplayName, m_CurrentUser.UserId);
            }

            try
            {
                AuthResult result = await m_Instance.SignInAnonymouslyAsync()
                    .AsUniTask()
                    .Timeout(TimeSpan.FromSeconds(5))
                    .AttachExternalCancellation(ReserveToken)
                    ;
                $"{result.User.DisplayName}, {result.User.UserId}".ToLog();

                m_CurrentUser       = result.User;
                m_CurrentCredential = result.Credential;

                var r = new UserInfo(m_CurrentUser.DisplayName, m_CurrentUser.UserId);
                await OnLoggedIn(r);

                return r;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        private async UniTask OnLoggedIn(UserInfo userInfo)
        {
            using var tempArray = TempArray<UniTask>.Shared(m_CallbacksList.Count);

            for (int i = 0; i < m_CallbacksList.Count; i++)
            {
                tempArray.Value[i] = m_CallbacksList[i].OnLoggedIn(userInfo)
                        .AttachExternalCancellation(ReserveToken)
                    ;
            }

            await UniTask.WhenAll(tempArray);
        }
        public void RegisterCallback(IAuthenticationCallbacks callbacks)
        {
            // TODO: Thread safe
            m_CallbacksList.Add(callbacks);

            if (LoggedIn)
                callbacks.OnLoggedIn(CurrentUserInfo)
                    .AttachExternalCancellation(ReserveToken)
                    .Forget()
                    ;
        }

        public void SignOut()
        {
            Assert.IsNotNull(m_CurrentUser);
            if (m_CurrentUser is null) return;

            if (m_CurrentUser.IsAnonymous)
            {
                DeleteAccountAsync().Forget();
                return;
            }
            m_CurrentUser = null;
            m_Instance.SignOut();
        }

        public async UniTask DeleteAccountAsync()
        {
            Assert.IsNotNull(m_CurrentUser);

            var user = m_CurrentUser;
            m_CurrentUser       = null;
            m_CurrentCredential = null;

            await user.DeleteAsync()
                .AsUniTask()
                .Timeout(TimeSpan.FromSeconds(5))
                .AttachExternalCancellation(ReserveToken)
                ;
            m_Instance.SignOut();
        }
    }
}