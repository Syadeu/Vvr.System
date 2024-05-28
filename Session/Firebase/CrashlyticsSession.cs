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
using Firebase.Crashlytics;
using JetBrains.Annotations;

namespace Vvr.Session.Firebase
{
    [UsedImplicitly]
    class CrashlyticsSession : ChildSession<CrashlyticsSession.SessionData>
    {
        // https://firebase.google.com/docs/crashlytics/get-started?_gl=1*1o55qxr*_up*MQ..*_ga*MTQxMjI4MDkxLjE3MTY4MTk0NzA.*_ga_CW55HF8NVT*MTcxNjgxOTQ3MC4xLjAuMTcxNjgxOTQ3MC4wLjAuMA..&platform=unity

        public struct SessionData : ISessionData
        {
            public FirebaseApp app;
        }

        public override string DisplayName => nameof(CrashlyticsSession);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            if (!VvrApplication.IsDevelopment)
            {
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                Crashlytics.IsCrashlyticsCollectionEnabled  = true;
            }

            return base.OnInitialize(session, data);
        }
    }
}