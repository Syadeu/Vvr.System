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
// File created : 2024, 05, 18 17:05

#endregion

using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
    partial class DefaultStage : IStageActorTagInOutProvider,
        IConnector<IGameTimeProvider>
    {
        private bool          m_CanParry, m_IsParrying;
        private RealtimeTimer m_EnemySkillStartedTime;
        private IActor  m_ParryEnemyTarget;

        private IGameTimeProvider m_GameTimeProvider;

        private async UniTask ParryEnemyActionScope(IEventTarget e, Condition condition, string value)
        {
            if (!ReferenceEquals(CurrentEventActor.Owner, e)) return;
            if (CurrentEventActor.Owner.ConditionResolver[Condition.IsPlayerActor](null))
                return;

            // Since ConditionTrigger always executed from main thread,
            // we don't need to think about memory barrier

            if (condition == Condition.OnSkillStart)
            {
                m_CanParry = false;

                var skillData = CurrentEventActor.Data.Skills.First(x => x.Id == value);
                if (IsInterruptibleSkill(skillData))
                {
                    m_CanParry              = true;
                    m_EnemySkillStartedTime = RealtimeTimer.Start();
                    m_ParryEnemyTarget      = (IActor)e;
                    m_GameTimeProvider.SetTimeScale(0.25f, 3);

                    "Set parry".ToLog();
                }
            }
            else if (condition is Condition.OnSkillEnd or Condition.OnSkillCasting)
            {
                m_CanParry         = false;
                m_ParryEnemyTarget = null;
                m_GameTimeProvider.Cancel();
            }
        }

        private static bool IsInterruptibleSkill(ISkillData skillData)
        {
            if ((skillData.Target & SkillSheet.Target.Enemy) != SkillSheet.Target.Enemy)
                return false;

            if (skillData.Position
                is SkillSheet.Position.All
                or SkillSheet.Position.Random
                or SkillSheet.Position.Forward)
                return true;

            return false;
        }

        UniTask IStageActorTagInOutProvider.TagIn(IActor actor)
        {
            Assert.IsNotNull(actor);

            int index = m_HandActors.FindIndex(x => ReferenceEquals(x.Owner, actor));
            if (index < 0)
            {
                "This actor is not in hand".ToLogError();
                return UniTask.CompletedTask;
            }

            return TagIn(index, ReserveToken);
        }

        private partial async UniTask TagIn(int index, CancellationToken cancellationToken)
        {
            Assert.IsFalse(index < 0);
            Assert.IsTrue(index  < m_HandActors.Count);

            IStageActor targetActor = m_HandActors[index];
            if (!targetActor.Owner.ConditionResolver[Condition.CanTag](null))
            {
                "Cant tag.".ToLog();
                return;
            }

            m_IsParrying = false;
            if (m_PlayerField.Count > 0 &&
                // Parrying in definition its happens when enemy attacks.
                // so if the target actor has turned, means cannot be parried.
                !targetActor.Owner.ConditionResolver[Condition.IsActorTurn](null))
            {
                // TODO: Check interruptible state
                // targetActor.State & ActorState.CanParry;

                if (!m_CanParry &&
                    m_EnemySkillStartedTime.IsExceeded(1))
                {
                    "not interruptible".ToLog();
                    return;
                }

                m_IsParrying = true;
            }

            m_HandActors.RemoveAt(index);

            using (var trigger = ConditionTrigger.Push(targetActor.Owner, ConditionTrigger.Game))
            {
                await trigger.Execute(Model.Condition.OnTagIn, targetActor.Owner.Id, cancellationToken);

                if (m_IsParrying)
                    await trigger.Execute(Condition.OnParrying, targetActor.Owner.Id, cancellationToken);

                if (m_PlayerField.Count > 0)
                {
                    IStageActor currentFieldRuntimeActor = m_PlayerField[0];
                    currentFieldRuntimeActor.TagOutRequested = true;

                    await JoinAfterAsync(currentFieldRuntimeActor, m_PlayerField, targetActor, cancellationToken);

                    m_TimelineQueueProvider.SetEnable(currentFieldRuntimeActor, false);
                    RemoveFromTimeline(currentFieldRuntimeActor, m_IsParrying ? 0 : 1);

                    await m_ViewProvider.ResolveAsync(currentFieldRuntimeActor.Owner)
                        .AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    Join(m_PlayerField, targetActor);
                }

                // View resolve requires ConditionTrigger scope.
                // Because of view uses Condition for resolving their position (front or back)
                await m_ViewProvider.ResolveAsync(targetActor.Owner).AttachExternalCancellation(cancellationToken);
                // TODO: show parrying text

                if (m_IsParrying)
                    m_GameTimeProvider.Cancel();
            }

            // This block requires for blocking turn table sequence
            // by m_IsParrying variable.
            if (m_IsParrying)
            {
                bool isSkillEnd = false;

                using var enemyOb = m_ParryEnemyTarget.ConditionResolver.CreateObserver();
                enemyOb[Condition.OnSkillEnd] = (owner, value, token) =>
                {
                    isSkillEnd = true;
                    return UniTask.CompletedTask;
                };

                while (!isSkillEnd && !cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Yield();
                }

                // After parrying, previous field actor should be tagged out.
                // normally, parrying will execute when is not user turn.
                await TagOut(m_PlayerField[0], cancellationToken);
            }

            UpdateTimeline();

            m_IsParrying = false;
        }

        private partial async UniTask TagOut(IStageActor target, CancellationToken cancelTokenSource)
        {
            Assert.IsTrue(target.TagOutRequested);
            Assert.IsTrue(target.Owner.ConditionResolver[Model.Condition.IsPlayerActor](null));

            using var trigger = ConditionTrigger.Push(target.Owner, ConditionTrigger.Game);

            m_PlayerField.Remove(target);
            m_HandActors.Add(target);

            target.TagOutRequested = false;
            RemoveFromQueue(target);

            await trigger.Execute(Condition.OnTagOut, target.Owner.Id, cancelTokenSource);

            await m_ViewProvider.ResolveAsync(target.Owner)
                .AttachExternalCancellation(cancelTokenSource);
            foreach (var actor in m_PlayerField)
            {
                await m_ViewProvider.ResolveAsync(actor.Owner)
                    .AttachExternalCancellation(cancelTokenSource);

                if (cancelTokenSource.IsCancellationRequested) break;
            }
        }

        void IConnector<IGameTimeProvider>.Connect(IGameTimeProvider    t) => m_GameTimeProvider = t;
        void IConnector<IGameTimeProvider>. Disconnect(IGameTimeProvider t) => m_GameTimeProvider = null;
    }
}