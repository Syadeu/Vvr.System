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
// File created : 2024, 06, 20 02:06

#endregion

using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.TestClass;

namespace Vvr.Session.Tests
{
    [UsedImplicitly]
    class HeavyDITestSession : TestParentSession,
        IConnector<ITestLocalProvider>,
        IConnector<ITestLocalProvider01>,
        IConnector<ITestLocalProvider02>,
        IConnector<ITestLocalProvider03>,
        IConnector<ITestLocalProvider04>
    {
        public override string DisplayName => nameof(HeavyDITestSession);

        public ITestLocalProvider Provider00 { get; private set; }
        public ITestLocalProvider01 Provider01 { get; private set; }
        public ITestLocalProvider02 Provider02 { get; private set; }
        public ITestLocalProvider03 Provider03 { get; private set; }
        public ITestLocalProvider04 Provider04 { get; private set; }

        void IConnector<ITestLocalProvider>.Connect(ITestLocalProvider    t) => Provider00 = t;
        void IConnector<ITestLocalProvider>.Disconnect(ITestLocalProvider t) => Provider00 = null;

        void IConnector<ITestLocalProvider01>.Connect(ITestLocalProvider01    t) => Provider01 = t;
        void IConnector<ITestLocalProvider01>.Disconnect(ITestLocalProvider01 t) => Provider01 = null;

        void IConnector<ITestLocalProvider02>.Connect(ITestLocalProvider02    t) => Provider02 = t;
        void IConnector<ITestLocalProvider02>.Disconnect(ITestLocalProvider02 t) => Provider02 = null;

        void IConnector<ITestLocalProvider03>.Connect(ITestLocalProvider03    t) => Provider03 = t;
        void IConnector<ITestLocalProvider03>.Disconnect(ITestLocalProvider03 t) => Provider03 = null;

        void IConnector<ITestLocalProvider04>.Connect(ITestLocalProvider04    t) => Provider04 = t;
        void IConnector<ITestLocalProvider04>.Disconnect(ITestLocalProvider04 t) => Provider04 = null;
    }
}