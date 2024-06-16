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
// File created : 2024, 06, 16 15:06
#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using Vvr.Provider;
using Vvr.Session.EventView.Core;

namespace Vvr.Session.EventView
{
    [UsedImplicitly]
    public sealed class EventViewSession : ParentSession<EventViewSession.SessionData>,
        IConnector<IViewRegistryProvider>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(EventViewSession);

        private IViewRegistryProvider m_ViewRegistryProvider;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Vvr.Provider.Provider.Static.Connect<IViewRegistryProvider>(this);
            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IViewRegistryProvider>(this);
            return base.OnReserve();
        }

        void IConnector<IViewRegistryProvider>.Connect(IViewRegistryProvider t)
        {
            Assert.NotNull(t);
            Assert.NotNull(t.Providers);

            foreach (var item in t.Providers)
            {
                Parent.Register(item.Key, item.Value);
            }
            m_ViewRegistryProvider = t;
        }

        void IConnector<IViewRegistryProvider>.Disconnect(IViewRegistryProvider t)
        {
            foreach (var item in t.Providers)
            {
                Parent.Unregister(item.Key);
            }

            m_ViewRegistryProvider = null;
        }
    }
}