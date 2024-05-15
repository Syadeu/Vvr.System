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
using Vvr.Controller.Input;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.UI.Observer;

namespace Vvr.Controller.Session.World
{
    [ParentSession(typeof(DefaultFloor), true), Preserve]
    public partial class DefaultStage : ChildSession<DefaultStage.SessionData>, IStageProvider,
        IConnector<IActorProvider>
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
        public sealed class RuntimeActor : IRuntimeActor
        {
            public readonly IActor         owner;
            public readonly ActorSheet.Row data;

            public bool tagOutRequested;

            public RuntimeActor(RuntimeActor x)
            {
                owner = x.owner;
                data  = x.data;
            }
            public RuntimeActor(IActor o, ActorSheet.Row d)
            {
                owner = o;
                data  = d;

                tagOutRequested = false;
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

        private IActorProvider             m_ActorProvider;
        private AsyncLazy<IEventViewProvider>    m_ViewProvider;
        private AsyncLazy<IInputControlProvider> m_InputControlProvider;

        private AssetController<AssetType> m_AssetController;

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
            Vvr.Provider.Provider.Static
                .Register<IStateConditionProvider>(this);

            m_ViewProvider         = Vvr.Provider.Provider.Static.GetLazyAsync<IEventViewProvider>();
            m_InputControlProvider = Vvr.Provider.Provider.Static.GetLazyAsync<IInputControlProvider>();
            m_AssetController      = new(this);

            m_EnemyId = Owner.Issue;

            m_AssetController.Connect<AssetLoadTaskProvider>(data.stage.Assets);

            Register<ITargetProvider>(this);
        }

        protected override UniTask OnReserve()
        {
            Unregister<ITargetProvider>();

            Vvr.Provider.Provider.Static
                .Unregister<IStateConditionProvider>(this);

            m_AssetController.Dispose();

            m_ViewProvider         = null;
            m_InputControlProvider = null;
            m_AssetController      = null;

            m_HandActors.Clear();
            m_PlayerField.Clear();
            m_EnemyField.Clear();

            m_Timeline.Clear();
            m_TimelineQueue.Clear();

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
                        IActor target = m_ActorProvider.Resolve(data).CreateInstance();
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
                IActor         target = m_ActorProvider.Resolve(data).CreateInstance();
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

            UpdateTimeline();
            ObjectObserver<ActorList>.ChangedEvent(m_Timeline);

            foreach (var item in m_PlayerField
                         .Concat<RuntimeActor>(m_HandActors)
                         .Concat(m_EnemyField)
                     )
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id);

                item.owner.ConnectTime();

                Connect<ITargetProvider>(item.owner.Skill);
                Connect<ITargetProvider>(item.owner.Passive);
            }

            while (m_Timeline.Count > 0 && m_PlayerField.Count > 0 && m_EnemyField.Count > 0)
            {
                $"Timeline count {m_Timeline.Count}".ToLog();

                m_ResetEvent = new();
                RuntimeActor current  = m_Timeline[0];
                Assert.IsFalse(current.owner.Disposed);

                using (var trigger = ConditionTrigger.Push(current.owner, ConditionTrigger.Game))
                {
                    await trigger.Execute(Model.Condition.OnActorTurn, null);
                    await UniTask.WaitForSeconds(1f);
                    await TimeController.Next(1);

                    ExecuteTurn(current).Forget();

                    await m_ResetEvent.Task;

                    await trigger.Execute(Model.Condition.OnActorTurnEnd, null);

                    // Tag out check
                    if (current.tagOutRequested)
                    {
                        Assert.IsTrue(current.owner.ConditionResolver[Model.Condition.IsPlayerActor](null));

                        m_PlayerField.Remove(current);
                        m_HandActors.Add(current);
                    }
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

                DequeueTimeline();
                ObjectObserver<ActorList>.ChangedEvent(m_Timeline);
            }

            foreach (var item in m_PlayerField
                         .Concat<RuntimeActor>(m_HandActors)
                         .Concat(m_EnemyField))
            {
                using var trigger = ConditionTrigger.Push(item.owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleEnd, Data.stage.Id);

                Disconnect<ITargetProvider>(item.owner.Skill);
                Disconnect<ITargetProvider>(item.owner.Passive);

                item.owner.DisconnectTime();
                item.owner.Skill.Clear();
                item.owner.Abnormal.Clear();
            }

            m_Timeline.Clear();
            "Stage end".ToLog();
            return new Result(GetCurrentPlayerActors(), GetCurrentEnemyActors());
        }

        private partial UniTask Join(ActorList                  field,  RuntimeActor actor);
        private partial UniTask JoinAfter(RuntimeActor          target, ActorList    field, RuntimeActor actor);
        private partial UniTask Delete(ActorList                field,  RuntimeActor actor);
        private partial UniTask RemoveFromQueue(RuntimeActor    actor);
        private partial UniTask RemoveFromTimeline(RuntimeActor actor, int preserveCount = 0);

        private partial void DequeueTimeline();
        private partial void UpdateTimeline();

        // TODO: Test auto play method
        public async UniTask ExecuteTurn(RuntimeActor runtimeActor)
        {
            var inputControl = await m_InputControlProvider;
            // AI
            if (!inputControl.CanControl(runtimeActor.owner))
            {
                "[Stage] AI control".ToLog();
                int count = runtimeActor.data.Skills.Count;
                var skill = runtimeActor.data.Skills[UnityEngine.Random.Range(0, count)].Ref;

                await runtimeActor.owner.Skill.Queue(skill);
            }
            else
            {
                "[Stage] player control".ToLog();
                await inputControl.TransferControl(runtimeActor.owner);
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
                RuntimeActor currentFieldRuntimeActor = m_PlayerField[0];
                currentFieldRuntimeActor.tagOutRequested = true;

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

            using (var trigger = ConditionTrigger.Push(m_HandActors[index].owner, ConditionTrigger.Game))
            {
                await trigger.Execute(Model.Condition.OnTagIn, null);
            }

            // Swap
            UpdateTimeline();

            ObjectObserver<ActorList>.ChangedEvent(m_HandActors);
            ObjectObserver<ActorList>.ChangedEvent(m_Timeline);
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

        void IConnector<IActorProvider>.Connect(IActorProvider t)
        {
            Assert.IsNull(m_ActorProvider);
            m_ActorProvider = t;
        }
        void IConnector<IActorProvider>.Disconnect()
        {
            m_ActorProvider = null;
        }
    }
}