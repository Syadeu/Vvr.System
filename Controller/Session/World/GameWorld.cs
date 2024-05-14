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
using Vvr.Provider;

namespace Vvr.Controller.Session.World
{
    public static class GameWorld
    {
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

            return world;
        }
    }
}