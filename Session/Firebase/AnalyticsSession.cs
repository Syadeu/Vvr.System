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
// File created : 2024, 05, 28 00:05

#endregion

using Cysharp.Threading.Tasks;
using Firebase;
using JetBrains.Annotations;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    [UniqueSession, ParentSession(typeof(FirebaseSession))]
    class AnalyticsSession : ChildSession<AnalyticsSession.SessionData>
    {
        // https://firebase.google.com/docs/analytics/unity/start

        public struct SessionData : ISessionData
        {
            public FirebaseApp app;
        }

        public override string DisplayName => nameof(AnalyticsSession);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            // global::Firebase.Analytics.FirebaseAnalytics.LogEvent(
            //     global::Firebase.Analytics.FirebaseAnalytics.EventLogin
            //     );

            return base.OnInitialize(session, data);
        }
    }
}