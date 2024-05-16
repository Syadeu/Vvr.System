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
// File created : 2024, 05, 17 03:05

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;
using Vvr.Session.World;

namespace Vvr.Session
{
    [UsedImplicitly]
    [ParentSession(typeof(DefaultFloor), true)]
    public sealed class StageActorCreateSession : ChildSession<StageActorCreateSession.SessionData>,
        IStageActorProvider
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(StageActorCreateSession);

        public IStageActor Create(IActor actor, ActorSheet.Row data)
        {
            StageActor result = new StageActor(actor, data);
            IActor     item   = result;
            Connect<IAssetProvider>(item.Assets)
                .Connect<IActorDataProvider>(item.Skill)
                .Connect<ITargetProvider>(item.Skill)
                .Connect<ITargetProvider>(item.Passive)
                .Connect<IEventConditionProvider>(item.ConditionResolver)
                .Connect<IStateConditionProvider>(item.ConditionResolver);

            item.ConnectTime();

            return result;
        }
        public void Reserve(IStageActor item)
        {
            Disconnect<IAssetProvider>(item.Assets)
                .Disconnect<IActorDataProvider>(item.Skill)
                .Disconnect<ITargetProvider>(item.Skill)
                .Disconnect<ITargetProvider>(item.Passive)
                .Disconnect<IEventConditionProvider>(item.ConditionResolver)
                .Disconnect<IStateConditionProvider>(item.ConditionResolver);

            item.DisconnectTime();
            item.Skill.Clear();
            item.Abnormal.Clear();
        }
    }
}