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
// File created : 2024, 05, 27 23:05
#endregion

using Cysharp.Threading.Tasks;
using Firebase;
using JetBrains.Annotations;
using UnityEngine.Analytics;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    public class FirebaseSession : ParentSession<FirebaseSession.SessionData>
    {
        // https://developers.google.com/unity/packages#vr

        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(FirebaseSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var result = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (result != DependencyStatus.Available)
            {
                $"[Firebase] {result}".ToLogError();
                return;
            }

            var app = FirebaseApp.DefaultInstance;
            await UniTask.WhenAll(
                // CreateSession<AuthSession>(new AuthSession.SessionData() { app = app }),
                CreateSession<FirestoreSession>(new FirestoreSession.SessionData() { app = app }),
                CreateSession<CrashlyticsSession>(new CrashlyticsSession.SessionData() { app = app }),
                CreateSession<AnalyticsSession>(new AnalyticsSession.SessionData() { app   = app })
            );
        }
    }
}