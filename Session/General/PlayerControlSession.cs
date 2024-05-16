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
// File created : 2024, 05, 16 23:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class PlayerControlSession : InputControlSession<PlayerControlSession.SessionData>
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(PlayerControlSession);
        public override          bool    CanControl(IEventTarget target)
        {
            return Owner == target.Owner;
        }

        protected override async UniTask OnControl(IEventTarget  target)
        {
            // TODO: testing
            if (target is IActor actor)
            {
                await actor.Skill.Queue(0);
            }
        }
    }
}