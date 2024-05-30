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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using Vvr.Controller;
using Vvr.Controller.Actor;
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
        IStageInfoProvider,
        IConnector<IViewRegistryProvider>,
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

            public bool TryGetActor(IActor actor, out IStageActor result)
            {
                for (int i = 0; i < Count; i++)
                {
                    result = this[i];
                    if (!ReferenceEquals(result.Owner, actor)) continue;

                    return true;
                }

                result = null;
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

                short xx = (short)x.Data.Type,
                    yy   = (short)y.Data.Type;

                if (xx < yy) return 1;
                return xx > yy ? -1 : 0;
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

        private IActorProvider        m_ActorProvider;
        private IStageActorProvider   m_StageActorProvider;
        private IInputControlProvider m_InputControlProvider;
        private IViewRegistryProvider m_ViewProvider;

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
                .Register<IStageInfoProvider>(this)
                ;

            m_EnemyId = Owner.Issue;

            Vvr.Provider.Provider.Static.Register<IStageActorTagInOutProvider>(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Unregister<IStageActorTagInOutProvider>(this);

            // While this session feeds multiple providers to parent session
            // These will not release after this session closed.
            // So we need to manually remove from parent session.
            Parent.Unregister<ITargetProvider>()
                .Unregister<IStateConditionProvider>()
                .Unregister<IEventConditionProvider>()
                .Unregister<IStageInfoProvider>()
                ;

            m_HandActors.Clear();
            m_PlayerField.Clear();
            m_EnemyField.Clear();

            m_Timeline.Clear();

            return base.OnReserve();
        }

        public async UniTask<Result> Start()
        {
            $"Stage start: {Data.stage.Id}".ToLog();

            // int time = 0;
            {
                int playerIndex = 0;
                if (Data.players == null)
                {
                    Assert.IsNotNull(Data.prevPlayers);
                    foreach (var prevActor in Data.prevPlayers)
                    {
                        IStageActor runtimeActor = m_StageActorProvider.Create(prevActor.Owner, prevActor.Data);

                        if (playerIndex != 0)
                        {
                            m_HandActors.Add(runtimeActor);
                            await m_ViewProvider.CardViewProvider.Resolve(prevActor.Owner);
                        }
                        else
                        {
                            await JoinAsync(m_PlayerField, runtimeActor);
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
                            await m_ViewProvider.CardViewProvider.Resolve(target);
                        }
                        else
                        {
                            await JoinAsync(m_PlayerField, runtimeActor);
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

                await JoinAsync(m_EnemyField, runtimeActor);
            }

            TimeController.ResetTime();

            UpdateTimeline();

            foreach (var item in m_PlayerField
                         .Concat<IStageActor>(m_HandActors)
                         .Concat(m_EnemyField)
                     )
            {
                using var trigger = ConditionTrigger.Push(item.Owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id);
            }

            float previousTime = 0;
            while (m_Timeline.Count > 0 && m_PlayerField.Count > 0 && m_EnemyField.Count > 0)
            {
                $"Timeline count {m_Timeline.Count}".ToLog();
                await UpdateTimelineNodeViewAsync();

                m_ResetEvent = new();
                IStageActor current = m_Timeline[0];
                Assert.IsFalse(current.Owner.Disposed);

                {
                    float sub = m_Times[0] - previousTime;
                    float p   = Mathf.FloorToInt(sub / 5 * 100) * 0.01f;
                    for (int i = 0; i < 4; i++)
                    {
                        await TimeController.Next(p);
                    }
                    await TimeController.Next(sub - (p * 4));
                }

                bool isPlayerActor = current.Owner.ConditionResolver[Condition.IsPlayerActor](null);
                if (isPlayerActor)
                {
                    foreach (var handActor in m_HandActors)
                    {
                        using var trigger = ConditionTrigger.Push(handActor.Owner, ConditionTrigger.Game);

                        await trigger.Execute(Condition.OnActorTurn, null);
                    }
                }

                // Because field can be cleared by delayed skills
                if (m_PlayerField.Count > 0 && m_EnemyField.Count > 0)
                {
                    using (var trigger = ConditionTrigger.Push(current.Owner, ConditionTrigger.Game))
                    {
                        await trigger.Execute(Model.Condition.OnActorTurn, null);
                        await UniTask.WaitForSeconds(1f);
                        // await TimeController.Next(1);

                        ExecuteTurn(current)
                            .SuppressCancellationThrow()
                            .AttachExternalCancellation(ReserveToken)
                            .Forget(UnityEngine.Debug.LogError);

                        await m_ResetEvent.Task;

                        await trigger.Execute(Model.Condition.OnActorTurnEnd, null);

                        // Tag out check
                        if (current.TagOutRequested)
                        {
                            Assert.IsTrue(current.Owner.ConditionResolver[Model.Condition.IsPlayerActor](null));

                            m_PlayerField.Remove(current);
                            m_HandActors.Add(current);

                            current.TagOutRequested = false;
                            RemoveFromQueue(current);

                            await trigger.Execute(Condition.OnTagOut, current.Owner.Id);

                            await m_ViewProvider.CardViewProvider.Resolve(current.Owner);
                            foreach (var actor in m_PlayerField)
                            {
                                await m_ViewProvider.CardViewProvider.Resolve(actor.Owner);
                            }
                        }
                    }
                }

                if (isPlayerActor)
                {
                    foreach (var handActor in m_HandActors)
                    {
                        using var trigger = ConditionTrigger.Push(handActor.Owner, ConditionTrigger.Game);

                        await trigger.Execute(Condition.OnActorTurnEnd, null);
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
                        var idx = m_HandActors.IndexOf(actor.First());
                        await TagIn(idx);

                        "add actor from hand".ToLog();
                        Assert.IsTrue(m_PlayerField.Count > 0);
                        await UniTask.WaitForSeconds(1);
                    }
                }

                previousTime = DequeueTimeline();
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

        async UniTask IStageInfoProvider.Delete(IActor actor)
        {
            IStageActor sta;
            if (m_HandActors.TryGetActor(actor, out sta))
                await DeleteAsync(m_HandActors, sta);
            else if (m_PlayerField.TryGetActor(actor, out sta))
                await DeleteAsync(m_PlayerField, sta);
            else if (m_EnemyField.TryGetActor(actor, out sta))
                await DeleteAsync(m_EnemyField, sta);
        }

        private partial UniTask JoinAsync(ActorList                 field,  IStageActor actor);
        private partial UniTask JoinAfterAsync(IStageActor          target, ActorList   field, IStageActor actor);
        private partial UniTask DeleteAsync(ActorList               field,  IStageActor actor);
        private partial void    RemoveFromQueue(IStageActor    actor);
        private partial void    RemoveFromTimeline(IStageActor actor, int preserveCount = 0);

        private partial float   DequeueTimeline();
        private partial void    UpdateTimeline();
        private partial UniTask UpdateTimelineNodeViewAsync();
        private partial UniTask CloseTimelineNodeViewAsync();

        private partial UniTask TagIn(int index);

        private async UniTask ExecuteTurn(IStageActor runtimeActor)
        {
            using var triggerEvent = ConditionTrigger.Scope(OnActorAction);

            if (m_InputControlProvider == null)
            {
                "[Stage] Waiting input controller".ToLog();
                while (m_InputControlProvider == null)
                {
                    await UniTask.Yield();
                }
            }

            // AI
            if (!m_InputControlProvider.CanControl(runtimeActor.Owner))
            {
                "[Stage] AI control".ToLog();
                int count = runtimeActor.Data.Skills.Count;
                var skill = runtimeActor.Data.Skills[UnityEngine.Random.Range(0, count)];

                await runtimeActor.Owner.Skill.Queue(skill);
            }
            else
            {
                "[Stage] player control".ToLog();
                await m_InputControlProvider.TransferControl(runtimeActor.Owner);
            }

            m_ResetEvent.TrySetResult();
        }

        private async UniTask OnActorAction(IEventTarget e, Model.Condition condition, string value)
        {
            if (e is not IActor) return;

            await CloseTimelineNodeViewAsync();

            await UniTask.WaitForSeconds(0.1f);

            if (condition == Condition.OnTagIn ||
                condition == Condition.OnTagOut)
            {
                await UpdateTimelineNodeViewAsync();
            }
        }

        private IEnumerable<IStageActor> GetCurrentPlayerActors()
        {
            return m_PlayerField.Concat(m_HandActors);
            // for (int i = 0; i < m_PlayerField.Count; i++)
            // {
            //     yield return (m_PlayerField[i]);
            // }
            // for (int i = 0; i < m_HandActors.Count; i++)
            // {
            //     yield return (m_HandActors[i]);
            // }
        }

        private IEnumerable<IStageActor> GetCurrentEnemyActors()
        {
            return m_EnemyField;
            // for (int i = 0; i < m_EnemyField.Count; i++)
            // {
            //     yield return (m_EnemyField[i]);
            // }
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

        void IConnector<IStageActorProvider>.  Connect(IStageActorProvider      t) => m_StageActorProvider = t;
        void IConnector<IStageActorProvider>.  Disconnect(IStageActorProvider   t) => m_StageActorProvider = null;
        void IConnector<IViewRegistryProvider>.Connect(IViewRegistryProvider    t) => m_ViewProvider = t;
        void IConnector<IViewRegistryProvider>.Disconnect(IViewRegistryProvider t) => m_ViewProvider = null;
    }
}