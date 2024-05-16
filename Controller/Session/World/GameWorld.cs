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
// File created : 2024, 05, 10 10:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Session.World
{
    public static class GameWorld
    {
        private static GameConfigProvider s_GameConfigProvider;
        private static ActorDataProvider  s_ActorDataProvider;

        public static IWorldSession World { get; private set; }

        public static async UniTask<TWorldSession> GetOrCreate<TWorldSession>(Owner owner)
            where TWorldSession : IWorldSession
        {
            if (World is TWorldSession w)
            {
                return w;
            }

            if (World != null) throw new NotImplementedException();

            var world = (TWorldSession)Activator.CreateInstance(typeof(TWorldSession));
            await world.Initialize(owner, null, null);

            World = world;

            if (s_GameConfigProvider != null)
            {
                World.Register<IGameConfigProvider>(s_GameConfigProvider);
            }
            if (s_ActorDataProvider != null)
            {
                World.Register<IActorDataProvider>(s_ActorDataProvider);
            }

            return world;
        }

        public static void RegisterActorData(ActorSheet sheet)
        {
            Assert.IsNull(s_ActorDataProvider);

            s_ActorDataProvider = new(sheet);
            World?.Register<IActorDataProvider>(s_ActorDataProvider);
        }
        public static void RegisterConfigs(GameConfigSheet sheet)
        {
            Assert.IsNull(s_GameConfigProvider);

            s_GameConfigProvider = Vvr.Provider.GameConfigProvider.Construct(sheet);
            World?.Register<IGameConfigProvider>(s_GameConfigProvider);
        }
    }
}