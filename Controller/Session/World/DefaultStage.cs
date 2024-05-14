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
using Vvr.Controller.Actor;
using Vvr.Controller.Asset;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.MPC.Provider;
using Vvr.UI.Observer;

namespace Vvr.Controller.Session.World
{
    [ParentSession(typeof(DefaultFloor), true), Preserve]
    public partial class DefaultStage : ChildSession<DefaultStage.SessionData>,
        IConnector<IEventTargetProvider>
    {
        private class ActorList : List<RuntimeActor>, IReadOnlyActorList
        {
            CachedActor IReadOnlyList<CachedActor>.this[int index] => new CachedActor(this[index]);

            public ActorList() : base()
            {
                ObjectObserver<ActorList>.Get(this).EnsureContainer();
            }

            public bool TryGetActor(string instanceId, out CachedActor actor)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].owner.GetInstanceID().ToString() == instanceId)
                    {
                        actor = new CachedActor(this[i]);
                        return true;
                    }
                }

                actor = default;
                return false;
            }

            public void CopyTo(IRuntimeActor[] array)
            {
                for (int i = 0; i < Count; i++)
                {
                    array[i] = this[i];
                }
            }
            IEnumerator<CachedActor> IEnumerable<CachedActor>.GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return new CachedActor(this[i]);
                }
            }
        }
        public struct SessionData : ISessionData
        {
            public readonly Owner playerId;

            public readonly StageSheet.Row   stage;
            public readonly ActorSheet.Row[] actors;

            public readonly IEnumerable<CachedActor> prevPlayers;
            public readonly IEnumerable<ActorSheet.Row> players;

            public SessionData(Owner id, StageSheet.Row data, IEnumerable<CachedActor> p)
            {
                playerId = id;

                stage = data;
                actors  = data.Actors.Select(x => x.Ref).ToArray();

                prevPlayers = p;
                players     = null;
            }
            public SessionData(Owner id, StageSheet.Row data, IEnumerable<ActorSheet.Row> p)
            {
                playerId = id;

                stage  = data;
                actors   = data.Actors.Select(x => x.Ref).ToArray();

                prevPlayers = null;
                players     = p;
            }
        }

        struct ActorPositionComparer : IComparer<IRuntimeActor>
        {
            public static readonly Func<IRuntimeActor, IRuntimeActor>      Selector = x => x;
            public static readonly IComparer<IRuntimeActor> Static   = default(ActorPositionComparer);

            public int Compare(IRuntimeActor x, IRuntimeActor y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                short xx = (short)x.Data.Type,
                    yy   = (short)y.Data.Type;

                if (xx < yy) return -1;
                return xx > yy ? 1 : 0;
            }
        }
        public sealed class RuntimeActor : IComparable<RuntimeActor>, IEquatable<RuntimeActor>, IRuntimeActor
        {
            public readonly IActor         owner;
            public readonly ActorSheet.Row data;

            public int  time;
            public bool removeRequested;

            public RuntimeActor(RuntimeActor x, int t)
            {
                owner = x.owner;
                data  = x.data;

                time = t;
            }
            public RuntimeActor(IActor o, ActorSheet.Row d)
            {
                owner = o;
                data  = d;

                time            = (int)o.Stats[StatType.SPD];
                removeRequested = false;
            }

            public int CompareTo(RuntimeActor other)
            {
                float xx = owner.Stats[StatType.SPD],
                    yy   = other.owner.Stats[StatType.SPD];

                if (Mathf.Approximately(xx, yy))
                {
                    if (time < other.time) return -1;
                    return 1;
                }

                if (xx < yy) return 1;
                return -1;
            }

            public bool Equals(RuntimeActor other)
            {
                return Equals(owner, other.owner);
            }

            public override bool Equals(object obj)
            {
                return obj is RuntimeActor other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (owner != null ? owner.GetHashCode() : 0);
            }

            IActor IRuntimeActor.  Owner => owner;
            ActorSheet.Row IRuntimeActor.Data  => data;
        }
        public struct Result
        {
            public readonly IEnumerable<CachedActor> playerActors;
            public readonly IEnumerable<CachedActor> enemyActors;

            public Result(IEnumerable<CachedActor> p, IEnumerable<CachedActor> e)
            {
                playerActors = p;
                enemyActors  = e;
            }
        }

        private IEventTargetProvider          m_EventTargetProvider;
        private AsyncLazy<IEventViewProvider> m_ViewProvider;

        private AssetController<AssetType> m_AssetController;

        private readonly PriorityQueue<RuntimeActor> m_Queue    = new();
        private readonly ActorList     m_Timeline = new();

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

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            MPC.Provider.Provider.Static
                .Register<ITargetProvider>(this)
                .Register<IStateConditionProvider>(this)
                .Register<IGameMethodProvider>(this);

            await MPC.Provider.Provider.Static.ConnectAsync<IEventTargetProvider>(this);

            m_ViewProvider    = MPC.Provider.Provider.Static.GetLazyAsync<IEventViewProvider>();
            m_AssetController = new(this);

            m_EnemyId = Owner.Issue;

            m_AssetController.Connect<AssetLoadTaskProvider>(data.stage.Assets);
        }

        protected override UniTask OnReserve()
        {
            MPC.Provider.Provider.Static
                .Unregister<ITargetProvider>(this)
                .Unregister<IStateConditionProvider>(this)
                .Unregister<IGameMethodProvider>(this);

            MPC.Provider.Provider.Static.Disconnect<IEventTargetProvider>(this);

            m_AssetController.Dispose();

            m_ViewProvider    = null;
            m_AssetController = null;

            m_HandActors.Clear();
            m_PlayerField.Clear();
            m_EnemyField.Clear();
            m_Queue.Clear();
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
                    foreach (var prevActor in Data.prevPlayers)
                    {
                        RuntimeActor runtimeActor = new(prevActor.Owner, prevActor.Data)
                        {
                            // time = time++
                        };

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
                        IActor target = m_EventTargetProvider.Resolve(data).CreateInstance();
                        target.Initialize(Data.playerId, data);

                        RuntimeActor runtimeActor = new(target, data)
                        {
                            // time = time++
                        };

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
            for (int i = 0; i < Data.actors.Length; i++)
            {
                ActorSheet.Row data   = Data.actors[i];
                IActor         target = m_EventTargetProvider.Resolve(data).CreateInstance();
                target.Initialize(m_EnemyId, data);

                RuntimeActor runtimeActor = new(target, data)
                {
                    // time = time++
                };

                await Join(m_EnemyField, runtimeActor);
            }

            ObjectObserver<ActorList>.ChangedEvent(m_HandActors);
            ObjectObserver<ActorList>.ChangedEvent(m_PlayerField);
            ObjectObserver<ActorList>.ChangedEvent(m_EnemyField);

            TimeController.ResetTime();

            AddActorsInOrderWithSpeed(5);
            ObjectObserver<ActorList>.ChangedEvent(m_Timeline);

            foreach (var item in m_PlayerField)
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id);
            }
            foreach (var item in m_HandActors)
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id);
            }
            foreach (var item in m_EnemyField)
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id);
            }

            while (m_Timeline.Count > 0 && m_PlayerField.Count > 0 && m_EnemyField.Count > 0)
            {
                $"Timeline count {m_Timeline.Count}".ToLog();

                m_ResetEvent = new();
                RuntimeActor currentRuntimeActor  = m_Timeline[0];
                Assert.IsFalse(currentRuntimeActor.owner.Disposed);

                using (var trigger = ConditionTrigger.Push(currentRuntimeActor.owner, ConditionTrigger.Game))
                {
                    await trigger.Execute(Model.Condition.OnActorTurn, null);
                    await UniTask.WaitForSeconds(1f);
                    await TimeController.Next(1);

                    ExecuteTurn(currentRuntimeActor).Forget();

                    await trigger.Execute(Model.Condition.OnActorTurnEnd, null);
                    await m_ResetEvent.Task;
                }

                // TODO: Should controlled by GameConfig?
                if (m_PlayerField.Count == 0)
                {
                    // Find alive actor
                    var actor = m_HandActors
                            .Where<RuntimeActor>(x => x.owner.Stats[StatType.HP] > 0)
                        ;
                    if (actor.Any())
                    {
                        await SwapPlayerCard(actor.First());
                        "add actor from hand".ToLog();
                        Assert.IsTrue(m_PlayerField.Count > 0);
                        await UniTask.WaitForSeconds(1);
                    }
                }

                //
                if (m_Timeline.Count > 0) m_Timeline.RemoveAt(0);
                AddActorsInOrderWithSpeed(10);
                ObjectObserver<ActorList>.ChangedEvent(m_Timeline);
            }

            foreach (var item in m_PlayerField)
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleEnd, Data.stage.Id);
            }

            foreach (var item in m_HandActors)
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleEnd, Data.stage.Id);
            }

            foreach (var item in m_EnemyField)
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleEnd, Data.stage.Id);
            }

            m_Timeline.Clear();
            "Stage end".ToLog();
            return new Result(GetCurrentPlayerActors(), GetCurrentEnemyActors());
        }

        private partial UniTask Join(ActorList   field, RuntimeActor actor);
        private partial UniTask Delete(ActorList field, RuntimeActor actor);

        // TODO: Test auto play method
        public async UniTask ExecuteTurn(RuntimeActor runtimeActor)
        {
            // AI
            if (!m_InputControlProvider.CanControl(runtimeActor.owner))
            {
                int count = runtimeActor.data.Skills.Count;
                var skill = runtimeActor.data.Skills[UnityEngine.Random.Range(0, count)].Ref;

                await runtimeActor.owner.Skill.Queue(skill);
            }
            else
            {
                await m_InputControlProvider.TransferControl(runtimeActor.owner);
            }

            m_ResetEvent.TrySetResult();
        }

        public async UniTask SwapPlayerCard(RuntimeActor d)
        {
            int index = m_HandActors.IndexOf(d);
            Assert.IsFalse(index < 0, "index < 0");
            await SwapPlayerCard(index);
        }
        public async UniTask SwapPlayerCard(int index)
        {
            if (m_PlayerField.Count > 1)
            {
                "Cant swap. already in progress".ToLog();
                return;
            }

            Assert.IsFalse(index < 0);
            Assert.IsTrue(index < m_HandActors.Count);

            int currentTime = m_Timeline[0].time;
            if (m_PlayerField.Count > 0)
            {
                RuntimeActor currentFieldRuntimeActor = m_PlayerField[0];

                m_Queue.RemoveAll(x => x.owner == currentFieldRuntimeActor.owner);
                int found = 0;
                for (int i = 0; i < m_Timeline.Count; i++)
                {
                    var e = m_Timeline[i];
                    if (e.owner != currentFieldRuntimeActor.owner) continue;

                    if (found++ < 1)
                    {
                        e.removeRequested = true;
                        m_Timeline[i]     = e;
                        continue;
                    }

                    m_Timeline.RemoveAt(i);
                    i--;
                }

                using (var trigger = ConditionTrigger.Push(currentFieldRuntimeActor.owner, ConditionTrigger.Game))
                {
                    await trigger.Execute(Model.Condition.OnTagOut, null);
                }
                using (var trigger = ConditionTrigger.Push(m_HandActors[index].owner, ConditionTrigger.Game))
                {
                    await trigger.Execute(Model.Condition.OnTagIn, null);
                }
            }

            // Swap
            var temp = m_HandActors[index];
            temp.time = currentTime;
            m_HandActors.RemoveAt(index);
            await Join(m_PlayerField, temp);

            m_Timeline.Insert(1, temp);
            // m_Timeline.Clear();
            // AddActorsInOrderWithSpeed(5);

            // if (IsInBattle)
            {
                // InsertActorInTimeline(
                //     new RuntimeActor(temp, currentTime),
                //     // temp.ToRuntimeActor(sheet, PlayerSystem.ActiveData.Id, m_LastOrder++, currentTime),
                //     // Mathf.Max(1, found - 1)
                //     5
                //     );
            }

            ObjectObserver<ActorList>.ChangedEvent(m_HandActors);
            ObjectObserver<ActorList>.ChangedEvent(m_Timeline);
        }

        private void InsertActorInTimeline(in RuntimeActor runtimeActor, int count)
        {
            if (count <= 0) return;

            RuntimeActor lastRuntimeActor  = runtimeActor;
            int targetTime = m_Timeline[0].time;
            for (int i = 0; i < m_Timeline.Count && 0 < count; i++)
            {
                RuntimeActor e = m_Timeline[i];

                if (targetTime < e.time)
                {
                    lastRuntimeActor = new RuntimeActor(lastRuntimeActor, targetTime);
                    m_Timeline.Insert(i, lastRuntimeActor);

                    $"Added {lastRuntimeActor.owner.DisplayName}, {lastRuntimeActor.removeRequested}".ToLog();
                    targetTime += (int)runtimeActor.owner.Stats[StatType.SPD];
                    count--;
                    i++;
                }
            }

            m_Queue.Enqueue(
                new RuntimeActor(lastRuntimeActor, targetTime),
                targetTime);
        }
        private void AddActorsInOrderWithSpeed(int count)
        {
            const int MAX_TIMELINE_COUNT = 5;

            if (m_Queue.Count == 0) return;

            int c = 0;
            while (c < count && m_Timeline.Count < MAX_TIMELINE_COUNT)
            {
                var kvp = m_Queue.Dequeue();

                m_Timeline.Add(new RuntimeActor(kvp.Key, (int)kvp.Value + 1));

                float nextActionTime = kvp.Value + kvp.Key.owner.Stats[StatType.SPD];
                m_Queue.Enqueue(
                    new RuntimeActor(kvp.Key, (int)nextActionTime),
                    nextActionTime);

                c++;
            }

            // ObjectObserver<BattleTable>.ChangedEvent(this, nameof(Timeline));
        }

        private IEnumerable<CachedActor> GetCurrentPlayerActors()
        {
            for (int i = 0; i < m_PlayerField.Count; i++)
            {
                yield return new CachedActor(m_PlayerField[i]);
            }
            for (int i = 0; i < m_HandActors.Count; i++)
            {
                yield return new CachedActor(m_HandActors[i]);
            }
        }

        private IEnumerable<CachedActor> GetCurrentEnemyActors()
        {
            for (int i = 0; i < m_EnemyField.Count; i++)
            {
                yield return new CachedActor(m_EnemyField[i]);
            }
        }

        void IConnector<IEventTargetProvider>.Connect(IEventTargetProvider t)
        {
            Assert.IsNull(m_EventTargetProvider);
            m_EventTargetProvider = t;
        }
        void IConnector<IEventTargetProvider>.Disconnect()
        {
            m_EventTargetProvider = null;
        }
    }
}