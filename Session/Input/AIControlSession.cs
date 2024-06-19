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
// File created : 2024, 05, 21 11:05

#endregion

using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Provider;

namespace Vvr.Session.Input
{
    [UsedImplicitly]
    public class AIControlSession : InputControlSession<AIControlSession.SessionData>,
        IConnector<IActorDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        protected IActorDataProvider   ActorDataProvider { get; private set; }

        public override string DisplayName => nameof(AIControlSession);

        public override bool CanControl(IEventTarget target)
        {
            return target is IActor;
        }

        protected override async UniTask OnControl(IEventTarget  target, CancellationToken cancellationToken)
        {
            IActor actor     = (IActor)target;
            var    actorData = ActorDataProvider.Resolve(actor.Id);

            // AI
            int count = actorData.Skills.Count;
            var skill = actorData.Skills[UnityEngine.Random.Range(0, count)];

            await actor.Skill.Queue(skill)
                .AttachExternalCancellation(cancellationToken);
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider    t) => ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => ActorDataProvider = null;
    }
}