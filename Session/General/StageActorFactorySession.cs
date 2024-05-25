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

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
    public sealed class StageActorFactorySession : ChildSession<StageActorFactorySession.SessionData>,
        IStageActorProvider,
        IConnector<IResearchDataProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        private sealed class StageActor : IStageActor
        {
            public IActor     Owner           { get; }
            public IActorData Data            { get; }
            public bool       TagOutRequested { get; set; }

            public StageActor(IActor o, IActorData d)
            {
                Owner = o;
                Data  = d;
            }

            void IConnector<IAssetProvider>.Connect(IAssetProvider t)
            {
                Owner.Assets.Connect(t);
            }

            void IConnector<IAssetProvider>.Disconnect(IAssetProvider t)
            {
                Owner.Assets.Disconnect(t);
            }

            void IConnector<ITargetProvider>.Connect(ITargetProvider t)
            {
                Owner.Skill.Connect(t);
                Owner.Passive.Connect(t);
            }

            void IConnector<ITargetProvider>.Disconnect(ITargetProvider t)
            {
                Owner.Skill.Disconnect(t);
                Owner.Passive.Disconnect(t);
            }

            void IConnector<IActorDataProvider>.Connect(IActorDataProvider t)
            {
                Owner.Skill.Connect(t);
            }

            void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t)
            {
                Owner.Skill.Disconnect(t);
            }

            void IConnector<IEventConditionProvider>.Connect(IEventConditionProvider t)
            {
                Owner.ConditionResolver.Connect(t);
            }

            void IConnector<IEventConditionProvider>.Disconnect(IEventConditionProvider t)
            {
                Owner.ConditionResolver.Disconnect(t);
            }

            void IConnector<IStateConditionProvider>.Connect(IStateConditionProvider t)
            {
                Owner.ConditionResolver.Connect(t);
            }

            void IConnector<IStateConditionProvider>.Disconnect(IStateConditionProvider t)
            {
                Owner.ConditionResolver.Disconnect(t);
            }

            void IConnector<IEventViewProvider>.Connect(IEventViewProvider t)
            {
                Owner.Skill.Connect(t);
            }
            void IConnector<IEventViewProvider>.Disconnect(IEventViewProvider t)
            {
                Owner.Skill.Disconnect(t);
            }
        }

        private readonly List<StageActor> m_Created = new();

        private IResearchDataProvider m_ResearchDataProvider;

        public override string DisplayName => nameof(StageActorFactorySession);

        protected override async UniTask OnReserve()
        {
            await base.OnReserve();

            using var timer = DebugTimer.Start();

            for (int i = m_Created.Count - 1; i >= 0; i--)
            {
                var e = m_Created[i];
                DisconnectActor(e);
            }
            m_Created.Clear();
        }

        public IStageActor Create(IActor actor, IActorData data)
        {
            using var timer = DebugTimer.Start();

            StageActor result = new StageActor(actor, data);
            IActor     item   = result.Owner;
            Connect<IAssetProvider>(result)
                .Connect<ITargetProvider>(result)
                .Connect<IEventViewProvider>(result)
                .Connect<IActorDataProvider>(result)
                .Connect<IEventConditionProvider>(result)
                .Connect<IStateConditionProvider>(result);

            item.ConnectTime();

            // Only if player actor
            if (actor.Owner == Owner)
            {
                foreach (var nodeGroup in m_ResearchDataProvider)
                {
                    foreach (var node in nodeGroup)
                    {
                        item.Stats.AddModifier(node);
                    }
                }
            }

            m_Created.Add(result);
            return result;
        }
        public void Reserve(IStageActor item)
        {
            using var timer = DebugTimer.Start();

            if (item is not StageActor actor ||
                !m_Created.Remove(actor))
                throw new InvalidOperationException();

            DisconnectActor(actor);
        }

        private void DisconnectActor(StageActor item)
        {
            using var timer = DebugTimer.Start();

            // Only if player actor
            if (item.Owner.Owner == Owner)
            {
                foreach (var nodeGroup in m_ResearchDataProvider)
                {
                    foreach (var node in nodeGroup)
                    {
                        item.Owner.Stats.RemoveModifier(node);
                    }
                }
            }

            Disconnect<IAssetProvider>(item)
                .Disconnect<ITargetProvider>(item)
                .Disconnect<IEventViewProvider>(item)
                .Disconnect<IActorDataProvider>(item)
                .Disconnect<IEventConditionProvider>(item)
                .Disconnect<IStateConditionProvider>(item);

            item.Owner.DisconnectTime();
            item.Owner.Skill.Clear();
            item.Owner.Abnormal.Clear();
        }

        void IConnector<IResearchDataProvider>.Connect(IResearchDataProvider    t) => m_ResearchDataProvider = t;
        void IConnector<IResearchDataProvider>.Disconnect(IResearchDataProvider t) => m_ResearchDataProvider = null;
    }
}