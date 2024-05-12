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
using Cysharp.Threading.Tasks;

namespace Vvr.System.Controller
{
    public static class GameWorld
    {
        internal static IWorldSession s_WorldSession;

        public static async UniTask<TWorldSession> GetOrCreate<TWorldSession>()
            where TWorldSession : IWorldSession
        {
            var world = (TWorldSession)Activator.CreateInstance(typeof(TWorldSession));
            await world.Initialize();

            s_WorldSession = world;

            return world;
        }

        private static async UniTaskVoid test()
        {
            DefaultWorld world = await GetOrCreate<DefaultWorld>();
            DefaultMap map = await world.CreateSession<DefaultMap>(new DefaultMap.Data());
            DefaultRegion region = await map.CreateSession<DefaultRegion>(default);
            DefaultFloor floor = await map.CreateSession<DefaultFloor>(default);
            // DefaultStage stage = await map.CreateSession<DefaultStage>(new StageSheet.Row());
        }
    }
}