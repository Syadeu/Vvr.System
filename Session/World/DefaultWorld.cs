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

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Provider.Command;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView;
using Vvr.Session.Provider;
#if VVR_FIREBASE
using Vvr.Session.Firebase;
#endif

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

        protected override UniTask OnInitialize(IParentSession session, RootData data)
        {
            Vvr.Provider.Provider.Static.Connect<IViewRegistryProvider>(this);

            return UniTask.CompletedTask;
        }

        [PublicAPI]
        public async UniTask Booting()
        {
            DataSession = await CreateSession<GameDataSession>(default);

            var results = await UniTask.WhenAll(
                CreateSession<CommandSession>(default),
                CreateSession<GameMethodResolveSession>(default),

                CreateSession<ContentViewSession>(default)
#if VVR_FIREBASE
                , CreateSession<FirebaseSession>(default)
#endif
            );

            await CreateSession<GameConfigResolveSession>(
                new GameConfigResolveSession.SessionData(MapType.Global, true));

            Register<ICommandProvider>(results.Item1)
                .Register<IGameMethodProvider>(results.Item2)
                ;

            DefaultMap = await CreateSession<DefaultMap>(default);
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<IViewRegistryProvider>(this);

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
                .Register<IEventTimelineNodeViewProvider>(t.TimelineNodeViewViewProvider)
                .Register<IStageViewProvider>(t.StageViewProvider)
                ;

            // ReSharper restore RedundantTypeArgumentsOfMethod
        }

        void IConnector<IViewRegistryProvider>.Disconnect(IViewRegistryProvider t)
        {
            Unregister<IEventViewProvider>()
                .Unregister<IEventTimelineNodeViewProvider>()
                .Unregister<IStageViewProvider>()
                ;
            m_ViewRegistryProvider = null;
        }
    }
}