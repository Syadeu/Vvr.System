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
// File created : 2024, 05, 10 16:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using Vvr.Controller;
using Vvr.Controller.Actor;
using Vvr.Controller.Asset;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;
using Vvr.UI.Observer;

namespace Vvr.Session.World
{
    [ParentSession(typeof(DefaultFloor), true), Preserve]
    public partial class DefaultStage : ChildSession<DefaultStage.SessionData>,
        IStageProvider,
        IConnector<IActorProvider>,
        IConnector<IStageActorProvider>,
        IConnector<IInputControlProvider>
    {
        private class ActorList : List<IStageActor>, IReadOnlyActorList
        {
            IStageActor IReadOnlyList<IStageActor>.this[int index] => (this[index]);

            public ActorList() : base()
            {
                ObjectObserver<ActorList>.Get(this).EnsureContainer();
            }

            public bool TryGetActor(string instanceId, out IStageActor actor)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].Owner.GetInstanceID().ToString() == instanceId)
                    {
                        actor = (this[i]);
                        return true;
                    }
                }

                actor = default;
                return false;
            }

            public new void CopyTo(IStageActor[] array)
            {
                for (int i = 0; i < Count; i++)
                {
                    array[i] = this[i];
                }
            }
            IEnumerator<IStageActor> IEnumerable<IStageActor>.GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return (this[i]);
                }
            }
        }
        public struct SessionData : ISessionData
        {
            public readonly IStageData              stage;
            public readonly IEnumerable<IActorData> actors;

            public readonly IEnumerable<IStageActor> prevPlayers;
            public readonly IEnumerable<IActorData>  players;

            public SessionData(IStageData data, IEnumerable<IStageActor> p)
            {
                stage  = data;
                actors = data.Actors;

                prevPlayers = p;
                players     = null;
            }
            public SessionData(IStageData data, IEnumerable<IActorData> p)
            {
                stage  = data;
                actors = data.Actors;

                prevPlayers = null;
                players     = p;
            }
        }

        struct ActorPositionComparer : IComparer<IStageActor>
        {
            public static readonly Func<IStageActor, IStageActor>      Selector = x => x;
            public static readonly IComparer<IStageActor> Static   = default(ActorPositionComparer);

            public int Compare(IStageActor x, IStageActor y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                short xx = (short)x.Type,
                    yy   = (short)y.Type;

                if (xx < yy) return -1;
                return xx > yy ? 1 : 0;
            }
        }

        public struct Result
        {
            public readonly IEnumerable<IStageActor> playerActors;
            public readonly IEnumerable<IStageActor> enemyActors;

            public Result(IEnumerable<IStageActor> p, IEnumerable<IStageActor> e)
            {
                playerActors = p;
                enemyActors  = e;
            }
        }

        private IActorProvider                m_ActorProvider;
        private IStageActorProvider           m_StageActorProvider;
        private IInputControlProvider         m_InputControlProvider;
        private AsyncLazy<IEventViewProvider> m_ViewProvider;

        private AssetController m_StageAssetController;

        private Owner m_EnemyId;

        private readonly ActorList
            m_HandActors  = new(),
            m_PlayerField = new(), m_EnemyField = new();

        private UniTaskCompletionSource m_ResetEvent;

        public override string DisplayName => nameof(DefaultStage);

        public IReadOnlyActorList Timeline    => m_Timeline;
        public IReadOnlyActorList HandActors  => m_HandActors;
        public IReadOnlyActorList PlayerField => m_PlayerField;
        public IReadOnlyActorList EnemyField  => m_EnemyField;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            // This is required for injecting actors
            Parent.Register<ITargetProvider>(this)
                .Register<IStateConditionProvider>(this)
                .Register<IEventConditionProvider>(this)
                .Register<IStageProvider>(this);

            // TODO: remove outside view provider
            m_ViewProvider         = Vvr.Provider.Provider.Static.GetLazyAsync<IEventViewProvider>();
            m_StageAssetController      = new(data.stage.Assets);

            // Connects stage asset to asset provider.
            // This makes use injected asset container from parent.
            // Which stages can share assets within same floor session.
            Connect<IAssetProvider>(m_StageAssetController);

            m_EnemyId = Owner.Issue;

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            // While this session feeds multiple providers to parent session
            // These will not release after this session closed.
            // So we need to manually remove from parent session.
            Parent.Unregister<ITargetProvider>()
                .Unregister<IStateConditionProvider>()
                .Unregister<IEventConditionProvider>()
                .Unregister<IStageProvider>();

            Disconnect<IAssetProvider>(m_StageAssetController);

            m_ViewProvider         = null;
            m_StageAssetController      = null;

            m_HandActors.Clear();
            m_PlayerField.Clear();
            m_EnemyField.Clear();

            m_Timeline.Clear();

            return base.OnReserve();
        }

        public async UniTask<Result> Start()
        {
            $"Stage start: {Data.stage.Id}".ToLog();
            var viewProvider = await m_ViewProvider;

            await viewProvider.Resolve(this);

            // int time = 0;
            {
                int playerIndex = 0;
                if (Data.players == null)
                {
                    Assert.IsNotNull(Data.prevPlayers);
                    foreach (var prevActor in Data.prevPlayers)
                    {
                        IStageActor runtimeActor = m_StageActorProvider.Create(prevActor.Owner, prevActor);

                        if (playerIndex != 0)
                        {
                            m_HandActors.Add(runtimeActor);
                            await viewProvider.Resolve(prevActor.Owner);
                        }
                        else
                        {
                            await Join(m_PlayerField, runtimeActor);
                        }

                        playerIndex++;
                    }
                }
                else
                {
                    foreach (var data in Data.players)
                    {
                        IActor target = m_ActorProvider.Resolve(data).CreateInstance();
                        target.Initialize(Owner, data);

                        IStageActor runtimeActor = m_StageActorProvider.Create(target, data);

                        if (playerIndex != 0)
                        {
                            m_HandActors.Add(runtimeActor);
                            await viewProvider.Resolve(target);
                        }
                        else
                        {
                            await Join(m_PlayerField, runtimeActor);
                        }

                        playerIndex++;
                    }
                }
            }
            foreach (var data in Data.actors)
            {
                IActor target = m_ActorProvider.Resolve(data).CreateInstance();
                target.Initialize(m_EnemyId, data);

                IStageActor runtimeActor = m_StageActorProvider.Create(target, data);

                await Join(m_EnemyField, runtimeActor);
            }

            // ObjectObserver<ActorList>.ChangedEvent(m_HandActors);
            // ObjectObserver<ActorList>.ChangedEvent(m_PlayerField);
            // ObjectObserver<ActorList>.ChangedEvent(m_EnemyField);

            TimeController.ResetTime();

            UpdateTimeline();
            // ObjectObserver<ActorList>.ChangedEvent(m_Timeline);

            foreach (var item in m_PlayerField
                         .Concat<IStageActor>(m_HandActors)
                         .Concat(m_EnemyField)
                     )
            {
                using var trigger = ConditionTrigger.Push(item.Owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id);
            }

            while (m_Timeline.Count > 0 && m_PlayerField.Count > 0 && m_EnemyField.Count > 0)
            {
                $"Timeline count {m_Timeline.Count}".ToLog();

                m_ResetEvent = new();
                IStageActor current  = m_Timeline[0];
                Assert.IsFalse(current.Owner.Disposed);

                using (var trigger = ConditionTrigger.Push(current.Owner, ConditionTrigger.Game))
                {
                    await trigger.Execute(Model.Condition.OnActorTurn, null);
                    await UniTask.WaitForSeconds(1f);
                    await TimeController.Next(1);

                    ExecuteTurn(current).Forget();

                    await m_ResetEvent.Task;

                    await trigger.Execute(Model.Condition.OnActorTurnEnd, null);

                    // Tag out check
                    if (current.TagOutRequested)
                    {
                        Assert.IsTrue(current.Owner.ConditionResolver[Model.Condition.IsPlayerActor](null));

                        m_PlayerField.Remove(current);
                        m_HandActors.Add(current);

                        current.TagOutRequested = false;
                    }
                }

                // TODO: Should controlled by GameConfig?
                if (m_PlayerField.Count == 0)
                {
                    // Find alive actor
                    var actor = m_HandActors
                            .Where<IStageActor>(x => x.Owner.Stats[StatType.HP] > 0)
                        ;
                    if (actor.Any())
                    {
                        await SwapPlayerCard(actor.First());
                        "add actor from hand".ToLog();
                        Assert.IsTrue(m_PlayerField.Count > 0);
                        await UniTask.WaitForSeconds(1);
                    }
                }

                DequeueTimeline();
                // ObjectObserver<ActorList>.ChangedEvent(m_Timeline);
            }

            foreach (var item in m_PlayerField
                         .Concat<IStageActor>(m_HandActors)
                         .Concat(m_EnemyField))
            {
                using var trigger = ConditionTrigger.Push(item.Owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleEnd, Data.stage.Id);

                m_StageActorProvider.Reserve(item);
            }

            m_Timeline.Clear();
            m_TimelineQueueProvider.Clear();
            "Stage end".ToLog();
            return new Result(GetCurrentPlayerActors(), GetCurrentEnemyActors());
        }

        private partial UniTask Join(ActorList                 field,  IStageActor actor);
        private partial UniTask JoinAfter(IStageActor          target, ActorList   field, IStageActor actor);
        private partial UniTask Delete(ActorList               field,  IStageActor actor);
        private partial UniTask RemoveFromQueue(IStageActor    actor);
        private partial UniTask RemoveFromTimeline(IStageActor actor, int preserveCount = 0);

        private partial void DequeueTimeline();
        private partial void UpdateTimeline();

        // TODO: Test auto play method
        private async UniTask ExecuteTurn(IStageActor runtimeActor)
        {
            // AI
            if (!m_InputControlProvider.CanControl(runtimeActor.Owner))
            {
                "[Stage] AI control".ToLog();
                int count = runtimeActor.Skills.Count;
                var skill = runtimeActor.Skills[UnityEngine.Random.Range(0, count)];

                await runtimeActor.Owner.Skill.Queue(skill);
            }
            else
            {
                "[Stage] player control".ToLog();
                await m_InputControlProvider.TransferControl(runtimeActor.Owner);
            }

            m_ResetEvent.TrySetResult();
        }

        private async UniTask SwapPlayerCard(IStageActor d)
        {
            int index = m_HandActors.IndexOf(d);
            Assert.IsFalse(index < 0, "index < 0");
            await SwapPlayerCard(index);
        }

        private async UniTask SwapPlayerCard(int index)
        {
            // Assert.IsTrue(m_Timeline.First.Value.actor.ConditionResolver[Model.Condition.IsPlayerActor](null));
            if (m_PlayerField.Count > 1)
            {
                "Cant swap. already in progress".ToLog();
                return;
            }

            Assert.IsFalse(index < 0);
            Assert.IsTrue(index < m_HandActors.Count);

            var temp = m_HandActors[index];
            m_HandActors.RemoveAt(index);

            if (m_PlayerField.Count > 0)
            {
                IStageActor currentFieldRuntimeActor = m_PlayerField[0];
                currentFieldRuntimeActor.TagOutRequested = true;

                await JoinAfter(currentFieldRuntimeActor, m_PlayerField, temp);

                await RemoveFromQueue(currentFieldRuntimeActor);
                await RemoveFromTimeline(currentFieldRuntimeActor, 1);

                // using (var trigger = ConditionTrigger.Push(currentFieldRuntimeActor.owner, ConditionTrigger.Game))
                // {
                //     await trigger.Execute(Model.Condition.OnTagOut, null);
                // }
            }
            else
            {
                await Join(m_PlayerField, temp);
            }

            using (var trigger = ConditionTrigger.Push(m_HandActors[index].Owner, ConditionTrigger.Game))
            {
                await trigger.Execute(Model.Condition.OnTagIn, null);
            }

            // Swap
            UpdateTimeline();

            // ObjectObserver<ActorList>.ChangedEvent(m_HandActors);
            // ObjectObserver<ActorList>.ChangedEvent(m_Timeline);
        }

        private IEnumerable<IStageActor> GetCurrentPlayerActors()
        {
            for (int i = 0; i < m_PlayerField.Count; i++)
            {
                yield return (m_PlayerField[i]);
            }
            for (int i = 0; i < m_HandActors.Count; i++)
            {
                yield return (m_HandActors[i]);
            }
        }

        private IEnumerable<IStageActor> GetCurrentEnemyActors()
        {
            for (int i = 0; i < m_EnemyField.Count; i++)
            {
                yield return (m_EnemyField[i]);
            }
        }

        void IConnector<IActorProvider>.Connect(IActorProvider t)
        {
            Assert.IsNull(m_ActorProvider);
            m_ActorProvider = t;
        }
        void IConnector<IActorProvider>.Disconnect(IActorProvider t)
        {
            Assert.IsTrue(ReferenceEquals(m_ActorProvider, t));
            m_ActorProvider = null;
        }

        void IConnector<IInputControlProvider>.Connect(IInputControlProvider t)
        {
            Assert.IsNull(m_InputControlProvider);
            m_InputControlProvider = t;
        }
        void IConnector<IInputControlProvider>.Disconnect(IInputControlProvider t)
        {
            Assert.IsNotNull(m_InputControlProvider);
            Assert.IsTrue(ReferenceEquals(m_InputControlProvider, t));
            m_InputControlProvider = null;
        }

        public void Connect(IStageActorProvider    t) => m_StageActorProvider = t;
        public void Disconnect(IStageActorProvider t) => m_StageActorProvider = null;
    }
}