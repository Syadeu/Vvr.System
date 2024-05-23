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
// File created : 2024, 05, 10 20:05

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using Vvr.Controller;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Provider;
using Vvr.Session.View;

namespace Vvr.Session.World
{
    [Preserve]
    public partial class DefaultWorld : RootSession, IWorldSession,
        IConnector<IViewRegistryProvider>
    {
        private IViewRegistryProvider m_ViewRegistryProvider;

        public override string DisplayName => nameof(DefaultWorld);

        public DefaultMap      DefaultMap  { get; private set; }
        public GameDataSession DataSession { get; private set; }

        protected override async UniTask OnInitialize(IParentSession session, RootData data)
        {
            Vvr.Provider.Provider.Static.Connect<IViewRegistryProvider>(this);

            DataSession = await CreateSession<GameDataSession>(default);
            var gameMethodResolver = await CreateSession<GameMethodResolveSession>(default);
            Register<IGameMethodProvider>(gameMethodResolver);

            await CreateSession<GameConfigResolveSession>(
                new GameConfigResolveSession.SessionData(MapType.Global, true));

            await CreateSession<ViewSession>(default);

            // TODO: skip map load
            DefaultMap = await CreateSession<DefaultMap>(default);
        }
        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IViewRegistryProvider>(this);

            Unregister<IActorProvider>();

            return base.OnReserve();
        }

        protected override UniTask OnCreateSession(IChildSession session)
        {
            session.Register(m_ViewRegistryProvider);

            return base.OnCreateSession(session);
        }

        void IConnector<IViewRegistryProvider>.Connect(IViewRegistryProvider    t)
        {
            m_ViewRegistryProvider = t;

            // ReSharper disable RedundantTypeArgumentsOfMethod

            Register<IEventViewProvider>(t.CardViewProvider)
                .Register<IDialogueViewProvider>(t.DialogueViewProvider)
                .Register<IEventTimelineNodeViewProvider>(t.TimelineNodeViewViewProvider)
                .Register<IStageViewProvider>(t.StageViewProvider)
                ;

            // ReSharper restore RedundantTypeArgumentsOfMethod
        }

        void IConnector<IViewRegistryProvider>.Disconnect(IViewRegistryProvider t)
        {
            Unregister<IEventViewProvider>()
                .Unregister<IDialogueViewProvider>()
                .Unregister<IEventTimelineNodeViewProvider>()
                .Unregister<IStageViewProvider>()
                ;
            m_ViewRegistryProvider = null;
        }
    }
}