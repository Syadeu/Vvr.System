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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
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
    public partial class DefaultStage : ParentSession<DefaultStage.SessionData>,
        IStageInfoProvider,
        IConnector<IActorViewProvider>,
        IConnector<IActorProvider>,
        IConnector<IActorDataProvider>,
        IConnector<IStageActorProvider>,
        IConnector<IInputControlProvider>
    {
        public struct SessionData : ISessionData
        {
            [NotNull] public readonly IStageData              stage;

            public readonly IEnumerable<IActor>     prevUserActors;
            public readonly IEnumerable<IActorData> userActors;

            public SessionData(IStageData data, IEnumerable<IActor> p)
            {
                stage  = data;

                prevUserActors = p;
                userActors     = null;
            }
            public SessionData(IStageData data, IEnumerable<IActorData> p)
            {
                stage  = data;

                prevUserActors = null;
                userActors     = p;
            }
        }

        // struct ActorPositionComparer : IComparer<IStageActor>
        // {
        //     public static readonly Func<IStageActor, IStageActor>      Selector = x => x;
        //     public static readonly IComparer<IStageActor> Static   = default(ActorPositionComparer);
        //
        //     public int Compare(IStageActor x, IStageActor y)
        //     {
        //         if (x == null && y == null) return 0;
        //         if (x == null) return 1;
        //         if (y == null) return -1;
        //
        //         short xx = (short)x.Data.Type,
        //             yy   = (short)y.Data.Type;
        //
        //         if (xx < yy) return 1;
        //         return xx > yy ? -1 : 0;
        //     }
        // }

        public struct Result
        {
            public readonly IEnumerable<IActor> playerActors;
            public readonly IEnumerable<IActor> enemyActors;

            public Result(IEnumerable<IActor> p, IEnumerable<IActor> e)
            {
                playerActors = p;
                enemyActors  = e;
            }
        }

        private IActorProvider        m_ActorProvider;
        private IActorDataProvider    m_ActorDataProvider;
        private IStageActorProvider   m_StageActorProvider;
        private IInputControlProvider m_InputControlProvider;
        private IActorViewProvider    m_ViewProvider;

        private Owner m_EnemyId;

        private StageActorFieldSession
            m_HandActors,
            m_PlayerField,
            m_EnemyField;

        private ITargetProvider m_ActorTargetSession;

        public override string DisplayName => nameof(DefaultStage);

        public IReadOnlyList<IStageActor> Timeline    => m_Timeline;
        public IReadOnlyActorList         HandActors  => m_HandActors;
        public IReadOnlyActorList         PlayerField => m_PlayerField;
        public IReadOnlyActorList         EnemyField  => m_EnemyField;

        public IStageActor CurrentEventActor { get; private set; }

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_EnemyId = Owner.Issue;

            EvaluateSessionData(data);

            m_HandActors  = await CreateSessionOnBackground<StageActorFieldSession>(default);
            m_PlayerField = await CreateSessionOnBackground<StageActorFieldSession>(default);
            m_EnemyField  = await CreateSessionOnBackground<StageActorFieldSession>(
                new StageActorFieldSession.SessionData()
                {
                    Owner = m_EnemyId
                });

            m_ActorTargetSession = await CreateSessionOnBackground<ActorTargetSession>(
                new ActorTargetSession.SessionData(m_PlayerField, m_EnemyField));

            // This is required for injecting actors
            Parent.Register<ITargetProvider>(m_ActorTargetSession)
                .Register<IStateConditionProvider>(this)
                .Register<IEventConditionProvider>(this)
                .Register<IStageInfoProvider>(this)
                ;

            Vvr.Provider.Provider.Static.Register<IStageActorTagInOutProvider>(this);
        }

        protected override UniTask OnReserve()
        {
            foreach (var item in m_PlayerField
                         .Concat<IStageActor>(m_HandActors)
                         .Concat(m_EnemyField))
            {
                m_StageActorProvider.Reserve(item);
            }

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

        [Conditional("UNITY_EDITOR")]
        private static void EvaluateSessionData(SessionData data)
        {
            if (data.stage is null)
                throw new InvalidOperationException("Stage data cannot be null");

            if (data.userActors is null)
            {
                if (data.prevUserActors is null)
                    throw new InvalidOperationException("Requires previous actors");
                foreach (var actor in data.prevUserActors)
                {
                    if (actor.Disposed)
                        throw new InvalidOperationException("One of previous user actor has been disposed");
                }
            }

            if (data.prevUserActors is null)
            {
                if (data.userActors is null)
                    throw new InvalidOperationException("Requires user actors");

                if (data.userActors.All(x => x is null))
                    throw new InvalidOperationException("Provided user actor is empty");
            }
        }

        private void SetupField()
        {
            int playerIndex = 0;
            if (Data.userActors == null)
            {
                Assert.IsNotNull(Data.prevUserActors);
                foreach (var prevActor in Data.prevUserActors)
                {
                    IStageActor runtimeActor = m_StageActorProvider.Create(prevActor, m_ActorDataProvider.Resolve(prevActor.Id));

                    if (playerIndex != 0)
                    {
                        m_HandActors.Add(runtimeActor);
                        m_ViewProvider.ResolveAsync(prevActor)
                            .AttachExternalCancellation(ReserveToken)
                            .SuppressCancellationThrow()
                            ;
                    }
                    else
                    {
                        Join(m_PlayerField, runtimeActor);
                    }

                    playerIndex++;
                }
            }
            else
            {
                foreach (var data in Data.userActors)
                {
                    if (data is null) continue;

                    IActor target = m_ActorProvider.Create(Owner, data);

                    IStageActor runtimeActor = m_StageActorProvider.Create(target, data);

                    if (playerIndex != 0)
                    {
                        m_HandActors.Add(runtimeActor);
                        m_ViewProvider.ResolveAsync(target)
                            .AttachExternalCancellation(ReserveToken)
                            .SuppressCancellationThrow()
                            ;
                    }
                    else
                    {
                        Join(m_PlayerField, runtimeActor);
                    }

                    playerIndex++;
                }
            }

            foreach (var data in Data.stage.Actors)
            {
                IActor target = m_ActorProvider.Create(m_EnemyId, data);

                IStageActor runtimeActor = m_StageActorProvider.Create(target, data);

                Join(m_EnemyField, runtimeActor);
            }
        }

        public async UniTask<Result> Start(CancellationToken cancellationToken)
        {
            $"Stage start: {Data.stage.Id}".ToLog();
            using var cancelTokenSource
                = CancellationTokenSource.CreateLinkedTokenSource(ReserveToken, cancellationToken);

            SetupField();

            TimeController.ResetTime();

            UpdateTimeline();

            await StartupStage(cancelTokenSource.Token);

            float previousTime = 0;
            while (m_Timeline.Count > 0 && m_PlayerField.Count > 0 && m_EnemyField.Count > 0 &&
                   !cancelTokenSource.IsCancellationRequested)
            {
                $"Timeline count {m_Timeline.Count}".ToLog();
                await UpdateTimelineNodeViewAsync(cancelTokenSource.Token);

                CurrentEventActor = m_Timeline[0];
                Assert.IsFalse(CurrentEventActor.Owner.Disposed);

                await ProceedTimeController(previousTime, cancelTokenSource.Token);

                bool isPlayerActor = CurrentEventActor.Owner.ConditionResolver[Condition.IsPlayerActor](null);
                if (isPlayerActor)
                {
                    foreach (var handActor in m_HandActors)
                    {
                        using var trigger = ConditionTrigger.Push(handActor.Owner, ConditionTrigger.Game);
                        await trigger.Execute(Condition.OnActorTurn, null, cancelTokenSource.Token);
                        if (cancelTokenSource.IsCancellationRequested) break;
                    }
                }

                UniTask turnTask;

                // Because field can be cleared by delayed skills while time controller.
                if (m_PlayerField.Count > 0 && m_EnemyField.Count > 0 &&
                    !cancelTokenSource.IsCancellationRequested)
                {
                    using var trigger = ConditionTrigger.Push(CurrentEventActor.Owner, ConditionTrigger.Game);
                    await trigger.Execute(Model.Condition.OnActorTurn, null, cancelTokenSource.Token);
                    await UniTask.WaitForSeconds(1f, cancellationToken: cancelTokenSource.Token);

                    turnTask = ExecuteTurn(CurrentEventActor, cancelTokenSource.Token);
                    while (!m_InputControlProvider.HasControlStarted &&
                           !cancelTokenSource.IsCancellationRequested)
                    {
                        await UniTask.Yield();
                    }
                    await m_InputControlProvider.WaitForEndControl;

                    await trigger.Execute(Model.Condition.OnActorTurnEnd, null, cancelTokenSource.Token);
                }
                else
                {
                    turnTask = UniTask.CompletedTask;
                }

                if (isPlayerActor)
                {
                    foreach (var handActor in m_HandActors)
                    {
                        using var trigger = ConditionTrigger.Push(handActor.Owner, ConditionTrigger.Game);
                        await trigger.Execute(Condition.OnActorTurnEnd, null, cancelTokenSource.Token);
                        if (cancelTokenSource.IsCancellationRequested) break;
                    }
                }

                await turnTask;

                // If currently is in parrying, wait for ends.
                // See TagIn method
                while (m_IsParrying &&
                       !cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Yield();
                }

                // Tag out check
                await CheckFieldTagOut(m_PlayerField, cancelTokenSource.Token);
                await CheckFieldTagOut(m_EnemyField, cancelTokenSource.Token);

                CurrentEventActor = null;

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
                        await TagIn(idx, cancelTokenSource.Token);

                        "add actor from hand".ToLog();
                        Assert.IsTrue(m_PlayerField.Count > 0);
                        await UniTask.WaitForSeconds(1, cancellationToken: cancelTokenSource.Token);
                    }
                }

                previousTime = DequeueTimeline();
            }

            IActor[] playerActors = GetCurrentPlayerActors().Select(x => x.Owner).ToArray();
            IActor[] enemyActors  = GetCurrentEnemyActors().Select(x => x.Owner).ToArray();

            foreach (var item in m_PlayerField
                         .Concat<IStageActor>(m_HandActors)
                         .Concat(m_EnemyField))
            {
                using var trigger = ConditionTrigger.Push(item.Owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleEnd, Data.stage.Id, cancelTokenSource.Token);

                m_StageActorProvider.Reserve(item);
            }

            m_Timeline.Clear();
            m_TimelineQueueProvider.Clear();

            m_HandActors.Clear();
            m_PlayerField.Clear();
            m_EnemyField.Clear();

            "Stage end".ToLog();
            return new Result(playerActors, enemyActors);
        }

        private async UniTask ProceedTimeController(float previousTime, CancellationToken cancellationToken)
        {
            float sub = m_Times[0] - previousTime;
            float p   = Mathf.FloorToInt(sub / 5 * 100) * 0.01f;
            for (int i = 0; i < 4; i++)
            {
                await TimeController.Next(p, cancellationToken);
            }

            await TimeController.Next(sub - (p * 4), cancellationToken);
        }
        private async UniTask StartupStage(CancellationToken cancellationToken)
        {
            foreach (var item in m_PlayerField
                         .Concat<IStageActor>(m_HandActors)
                         .Concat(m_EnemyField)
                    )
            {
                using var trigger = ConditionTrigger.Push(item.Owner, ConditionTrigger.Game);
                await trigger.Execute(Model.Condition.OnBattleStart, Data.stage.Id, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;
            }
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

        private partial void    Join(IStageActorField          field,  IStageActor      actor);
        private partial void    JoinAfter(IStageActor          target, IStageActorField field, IStageActor actor);
        private partial UniTask DeleteAsync(IList<IStageActor> field,  IStageActor      stageActor);
        private partial void    RemoveFromQueue(IStageActor    actor);
        private partial void    RemoveFromTimeline(IStageActor actor, int preserveCount = 0);

        private partial float   DequeueTimeline();
        private partial void    UpdateTimeline();
        private partial UniTask UpdateTimelineNodeViewAsync(CancellationToken cancellationToken);
        private partial UniTask CloseTimelineNodeViewAsync(CancellationToken cancellationToken = default);

        private partial UniTask TagIn(int                         index,  CancellationToken cancellationToken);
        private partial UniTask TagOut(IStageActor                target, CancellationToken cancelTokenSource);
        private partial UniTask CheckFieldTagOut(IStageActorField field,  CancellationToken cancellationToken);

        private async UniTask ExecuteTurn(IStageActor runtimeActor, CancellationToken cancellationToken)
        {
            // using var triggerEvent = ConditionTrigger.Scope(OnActorAction, nameof(OnActorAction));

            if (m_InputControlProvider == null)
            {
                "[Stage] Waiting input controller".ToLog();
                while (m_InputControlProvider == null &&
                       !cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Yield();
                }
            }

            if (cancellationToken.IsCancellationRequested) return;
            Assert.IsNotNull(m_InputControlProvider);

            // AI
            if (!m_InputControlProvider.CanControl(runtimeActor.Owner))
            {
                "[Stage] AI control".ToLog();
                using var scope = ConditionTrigger.Scope(ParryEnemyActionScope, nameof(ParryEnemyActionScope));

                int count = runtimeActor.Data.Skills.Count;
                var skill = runtimeActor.Data.Skills[UnityEngine.Random.Range(0, count)];

                await runtimeActor.Owner.Skill.QueueAsync(skill)
                    .AttachExternalCancellation(cancellationToken);
            }
            else
            {
                "[Stage] player control".ToLog();

                using var scope = ConditionTrigger.Scope(ParryEnemyActionScope, nameof(ParryEnemyActionScope));
                await m_InputControlProvider.TransferControl(runtimeActor.Owner, cancellationToken);
            }

            "[Stage] control done".ToLog();
            // m_ResetEvent.TrySetResult();
        }

        private IEnumerable<IStageActor> GetCurrentPlayerActors()
        {
            return m_PlayerField.Concat(m_HandActors);
        }

        private IEnumerable<IStageActor> GetCurrentEnemyActors()
        {
            return m_EnemyField;
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

        void IConnector<IStageActorProvider>.Connect(IStageActorProvider    t) => m_StageActorProvider = t;
        void IConnector<IStageActorProvider>.Disconnect(IStageActorProvider t) => m_StageActorProvider = null;
        void IConnector<IActorViewProvider>. Connect(IActorViewProvider     t) => m_ViewProvider = t;
        void IConnector<IActorViewProvider>. Disconnect(IActorViewProvider  t) => m_ViewProvider = null;
        void IConnector<IActorDataProvider>. Connect(IActorDataProvider     t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;
    }
}