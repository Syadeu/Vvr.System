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
// File created : 2024, 05, 07 19:05

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cathei.BakingSheet;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Buffer;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Controller.Skill
{
    public sealed partial class SkillController : ITimeUpdate, IDisposable, ISkill
    {
        struct Value : ITargetDefinition
        {
            public readonly Hash       hash;
            public readonly ISkillData skill;

            public float delayedExecutionTime;

            public readonly IActor overrideTarget;

            public Value(ISkillData d, IActor ta)
            {
                hash                 = new Hash(d.Id);
                skill                 = d;
                delayedExecutionTime = d.Delay;
                overrideTarget       = ta;
            }

            public SkillSheet.Target   Target   => skill.Target;
            public SkillSheet.Position Position => skill.Position;
        }

        readonly struct SkillExecutionScope : IDisposable
        {
            private readonly SkillController m_Ctr;

            public SkillExecutionScope(SkillController ctr)
            {
                m_Ctr             = ctr;
                m_Ctr.IsInExecution = true;
            }

            public void Dispose()
            {
                m_Ctr.IsInExecution = false;
            }
        }

        private IActorViewProvider m_ViewProvider;
        private IActorDataProvider m_DataProvider;
        private ITargetProvider    m_TargetProvider;

        private readonly List<Value> m_Values = new(2);

        private readonly List<Hash>              m_SkillCooltimeKeys = new(2);
        private readonly Dictionary<Hash, float> m_SkillCooltimes    = new(2);

        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        private IActor Owner         { get; }
        private bool   IsInExecution { get; set; }
        private bool   Disposed      { get; set; }

        private CancellationToken CancellationToken => m_CancellationTokenSource.Token;

        public SkillController(IActor o)
        {
            Owner = o;
        }
        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            m_CancellationTokenSource.Cancel();

            Clear();

            m_DataProvider   = null;
            m_TargetProvider = null;

            Disposed = true;
        }

        public void Clear()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            m_SkillCooltimeKeys.Clear();
            m_SkillCooltimes.Clear();
            m_Values.Clear();
        }

        float ISkill.GetSkillCooltime(ISkillID skill)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            Hash hash = new Hash(skill.Id);
            m_SkillCooltimes.TryGetValue(hash, out float v);
            return v;
        }
        int ISkill.GetSkillCount()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            var data         = m_DataProvider.Resolve(Owner.Id);
            return data.Skills.Count(x => x != null);
        }

        IUniTaskAsyncEnumerable<ISkillData> ISkill.GetSkills()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            return UniTaskAsyncEnumerable.Create<ISkillData>(async (wr, token) =>
            {
                var data = m_DataProvider.Resolve(Owner.Id);

                foreach (var skill in data.Skills)
                {
                    if (skill != null)
                    {
                        await wr.YieldAsync(skill)
                            .AttachExternalCancellation(token);
                    }
                }
            });
        }
        UniTask ISkill.QueueAsync(int index)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            var data = m_DataProvider.Resolve(Owner.Id);
            return QueueAsync(data.Skills[index]);
        }
        public UniTask QueueAsync(ISkillID skill)
        {
            return QueueAsync(skill, null);
        }

        public async UniTask QueueAsync(ISkillID data, IActor specifiedTarget)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));
            if (data is null)
                throw new InvalidOperationException();

            Assert.IsNotNull(data);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));
            Assert.IsFalse(m_Values.Any(x=>x.skill == data),
                $"Skill {data.Id} already queued but trying to queue another one. {m_Values.Count}");

            if (IsInExecution)
                throw new InvalidOperationException("Skill is in execution");

            $"[Skill:{Owner.DisplayName}]: SKILL QUEUED {data.Id}, {m_Values.Count}".ToLog();

            using var skillExecutionScope = new SkillExecutionScope(this);

            ISkillData skillRow;
            if (data is ISkillData row) skillRow = row;
            else
            {
                var skillData = m_DataProvider.Resolve(Owner.Id);
                skillRow = skillData.Skills.FirstOrDefault(x => x.Id == data.Id);
            }

            var value = new Value(skillRow, specifiedTarget);
            if (specifiedTarget is not null)
            {
                using var targetTrigger = ConditionTrigger.Push(specifiedTarget, ConditionTrigger.Skill);
                await targetTrigger.Execute(Model.Condition.OnTargeted, data.Id, CancellationToken);
            }

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Skill);
            await ExecuteSkillAsync(trigger, value)
                .AttachExternalCancellation(CancellationToken);
        }

        async UniTask ExecuteSkillAsync(ConditionTrigger trigger, Value value)
        {
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            // Check cooltime
            if (m_SkillCooltimes.TryGetValue(value.hash, out float cooltime))
            {
                $"[Skill:{Owner.DisplayName}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) is in cooltime {cooltime}"
                    .ToLog();

                await trigger.Execute(Model.Condition.OnSkillCooltime, $"{cooltime}", CancellationToken);
                return;
            }

            $"[Skill:{Owner.DisplayName}:{Owner.GetInstanceID()}] Skill start {value.skill.Id}".ToLog();

            // await trigger.Execute(Model.Condition.OnSkillStart, value.skill.Id, CancellationToken);
            if (CancellationToken.IsCancellationRequested)
                return;

            UniTask skillEffectTask = UniTask.CompletedTask;

            #region Warmup

            var viewTarget       = await m_ViewProvider.ResolveAsync(Owner);
            var skillEventHandle = viewTarget.GetComponent<ISkillEventHandler>();
            if (skillEventHandle is not null &&
                value.skill.SelfEffectAssetKey is not null)
            {
                SkillEffectEmitter emitter = new SkillEffectEmitter(value.skill.SelfEffectAssetKey, null);
                skillEffectTask = skillEventHandle
                    .OnSkillStart(value.skill, emitter)
                    .AttachExternalCancellation(CancellationToken)
                    .TimeoutWithoutException(TimeSpan.FromSeconds(5))
                    ;
            }
            else if (value.skill.SelfEffectAssetKey is not null)
            {
                Transform view = await m_ViewProvider.ResolveAsync(Owner);

                var             effectPool = GameObjectPool.GetWithRawKey(value.skill.SelfEffectAssetKey);
                skillEffectTask = effectPool
                    .SpawnEffect(view.position, Quaternion.identity)
                    .AttachExternalCancellation(CancellationToken)
                    ;
                // while (!effect.Reserved &&
                //        !CancellationToken.IsCancellationRequested)
                // {
                //     await UniTask.Yield();
                // }
            }

            #endregion

            await UniTask.WhenAll(
                skillEffectTask,
                trigger.Execute(Model.Condition.OnSkillStart, value.skill.Id, CancellationToken)
            );
            if (CancellationToken.IsCancellationRequested)
                return;

            // Check value can be executed immediately
            if (value.delayedExecutionTime > 0)
            {
                m_Values.Add(value);
                return;
            }

            if (skillEventHandle != null &&
                value.skill.CastingEffectAssetKey is not null)
            {
                SkillEffectEmitter emitter = new SkillEffectEmitter(value.skill.CastingEffectAssetKey, null);
                await skillEventHandle
                        .OnSkillCasting(value.skill, emitter)
                        .SuppressCancellationThrow()
                        .AttachExternalCancellation(CancellationToken)
                        .TimeoutWithoutException(TimeSpan.FromSeconds(5))
                    ;

                if (CancellationToken.IsCancellationRequested)
                    return;
            }

            await ExecuteSkillBody(trigger, value)
                    .AttachExternalCancellation(CancellationToken)
                ;
        }

        async UniTask ExecuteSkillBody(ConditionTrigger trigger, Value value)
        {
            Assert.IsFalse(Owner.Disposed);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            bool executed = false;
            if (value.overrideTarget is not null)
            {
                if (!value.overrideTarget.Disposed)
                {
                    using var targetTrigger = ConditionTrigger.Push(value.overrideTarget, ConditionTrigger.Skill);
                    await targetTrigger.Execute(Model.Condition.OnTargeted, value.skill.Id, CancellationToken);
                    // await UniTask.WaitForSeconds(2);

                    executed = true;
                    await ExecuteSkillTarget(trigger, value, value.overrideTarget)
                            .AttachExternalCancellation(CancellationToken)
                        ;
                }
            }
            else
            {
                int count          = 0;
                foreach (var target in m_TargetProvider.FindTargets(Owner, value))
                {
                    // If target count exceed its capability
                    if (value.skill.TargetCount <= count++) break;

                    using var targetTrigger = ConditionTrigger.Push(target, ConditionTrigger.Skill);
                    await targetTrigger.Execute(Model.Condition.OnTargeted, value.skill.Id, CancellationToken);
                    // await UniTask.WaitForSeconds(2);

                    await ExecuteSkillTarget(trigger, value, target)
                            .AttachExternalCancellation(CancellationToken)
                        ;
                    executed = true;
                }
            }

            // Target not found
            if (!executed)
            {
                Transform view = await m_ViewProvider.ResolveAsync(Owner);

                await view
                        .GetComponent<ISkillEventHandler>()
                        .OnSkillCanceled(value.skill)
                        .SuppressCancellationThrow()
                        .AttachExternalCancellation(CancellationToken)
                        .TimeoutWithoutException(TimeSpan.FromSeconds(5))
                    ;
            }

            RegisterCooltime(value);
            $"[Skill:{Owner.DisplayName}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) cooltime: {m_SkillCooltimes[value.hash]}".ToLog();
        }

        async UniTask ExecuteSkillTarget(ConditionTrigger trigger, Value value, IActor target)
        {
            Assert.IsFalse(target.Disposed);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            #region Warmup

            Transform targetView = await m_ViewProvider.ResolveAsync(target)
                .AttachExternalCancellation(CancellationToken);
            Transform view       = await m_ViewProvider.ResolveAsync(Owner)
                .AttachExternalCancellation(CancellationToken);

            var skillEventHandle = view.GetComponent<ISkillEventHandler>();
            if (skillEventHandle != null)
            {
                SkillEffectEmitter emitter = null;

                if (value.skill.TargetEffectAssetKey is not null)
                {
                    // If method is damage, should shake camera
                    if (value.skill.Method == SkillSheet.Method.Damage)
                    {
                        ICameraShakeProvider camPrv = await Vvr.Provider.Provider.Static.GetAsync<ICameraShakeProvider>()
                                .AttachExternalCancellation(CancellationToken)
                            ;

                        emitter = new SkillEffectEmitter(
                            value.skill.TargetEffectAssetKey,
                            () => camPrv.Shake().Forget());
                    }
                    else emitter = new SkillEffectEmitter(value.skill.TargetEffectAssetKey, null);
                }

                await skillEventHandle
                        .OnSkillEnd(value.skill, targetView, emitter)
                        .SuppressCancellationThrow()
                        .AttachExternalCancellation(CancellationToken)
                        .TimeoutWithoutException(TimeSpan.FromSeconds(5))
                    ;

                emitter?.Dispose();
            }
            else if (value.skill.TargetEffectAssetKey is not null)
            {
                var effectPool = GameObjectPool.GetWithRawKey(value.skill.TargetEffectAssetKey);
                var effect = await effectPool
                        .SpawnEffect(targetView.position, Quaternion.identity)
                        .AttachExternalCancellation(CancellationToken)
                    ;
                while (!effect.Reserved &&
                       !CancellationToken.IsCancellationRequested)
                {
                    await UniTask.Yield();
                }
            }

            #endregion

            #region Execution body

            using (var targetTrigger = ConditionTrigger.Push(target, ConditionTrigger.Skill))
            {
                for (int i = 0; i < value.skill.Abnormal?.Count; i++)
                {
                    AbnormalSheet.Row e = value.skill.Abnormal[i].Ref;

                    await target.Abnormal.AddAsync(e);
                }

                float dmg = Owner.Stats[StatType.ATT] * value.skill.Multiplier;
                switch (value.skill.Method)
                {
                    case SkillSheet.Method.Damage:
                        target.Stats.Push<DamageProcessor>(
                            value.skill.TargetStat, dmg);
                        await targetTrigger.Execute(Model.Condition.OnHit, null, CancellationToken);

                        break;
                    case SkillSheet.Method.Default:
                    default:
                        target.Stats.Push(
                            value.skill.TargetStat, dmg);

                        await targetTrigger.Execute(Model.Condition.OnHit, null, CancellationToken)
                            ;
                        break;
                }

                $"skill dmg with: {Owner.Stats[StatType.ATT]} * {value.skill.Multiplier}".ToLog();
            }

            #endregion

            await trigger.Execute(Model.Condition.OnSkillEnd, value.skill.Id, CancellationToken)
                ;

            $"[Skill:{Owner.Owner}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) executed to {target.GetInstanceID()}({target.Owner})".ToLog();
        }

        private void RegisterCooltime(in Value value)
        {
            Assert.IsFalse(m_SkillCooltimes.ContainsKey(value.hash));

            m_SkillCooltimes[value.hash] = value.skill.Cooltime;
            m_SkillCooltimeKeys.Add(value.hash);
        }

        UniTask ITimeUpdate.OnUpdateTime(float currentTime, float deltaTime)
        {
            for (int i = m_SkillCooltimeKeys.Count - 1; i >= 0; i--)
            {
                var key = m_SkillCooltimeKeys[i];
                if (!m_SkillCooltimes.TryGetValue(key, out var cooltime))
                {
                    // XXX: ???
                    $"Skill cooltime not found but has keys in: somethings went wrong {key}".ToLog();
                    m_SkillCooltimeKeys.RemoveAt(i);
                    continue;
                }

                float duration = cooltime - deltaTime;
                if (duration <= 0)
                {
                    m_SkillCooltimes.Remove(key);
                    m_SkillCooltimeKeys.RemoveAt(i);
                    continue;
                }

                m_SkillCooltimes[key] = duration;
            }

            // Update delay only if this actor turn
            // if (!Owner.ConditionResolver[Condition.IsActorTurn](null)) return;

            for (int i = 0; i < m_Values.Count; i++)
            {
                Value e = m_Values[i];
                e.delayedExecutionTime -= deltaTime;
                m_Values[i]            =  e;
            }

            return UniTask.CompletedTask;
        }
        async UniTask ITimeUpdate.OnEndUpdateTime()
        {
            Assert.IsFalse(Owner.Disposed);

            if (!Owner.ConditionResolver[Model.Condition.IsActorTurn](null))
            {
                // $"22. not this actor turn. skip {Owner.DisplayName}".ToLog();
                return;
            }

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Skill);

            for (int i = 0; i < m_Values.Count; i++)
            {
                Value e = m_Values[i];

                await trigger.Execute(Model.Condition.OnSkillCasting, e.skill.Id)
                    .AttachExternalCancellation(CancellationToken);

                Transform viewTarget       = await m_ViewProvider.ResolveAsync(Owner)
                    .AttachExternalCancellation(CancellationToken);
                var skillEventHandle = viewTarget.GetComponent<ISkillEventHandler>();

                if (skillEventHandle != null &&
                    e.skill.CastingEffectAssetKey is not null)
                {
                    SkillEffectEmitter emitter = new SkillEffectEmitter(e.skill.CastingEffectAssetKey, null);
                    await skillEventHandle
                            .OnSkillCasting(e.skill, emitter)
                            .SuppressCancellationThrow()
                            .AttachExternalCancellation(viewTarget.GetCancellationTokenOnDestroy())
                            .TimeoutWithoutException(TimeSpan.FromSeconds(5))
                        ;
                }

                // If time completed, execute
                if (e.delayedExecutionTime > 0)
                {
                    $"[Skill:{Owner.DisplayName}] {e.skill.Id} time left {e.delayedExecutionTime}".ToLog();
                    continue;
                }

                await ExecuteSkillBody(trigger, e);

                m_Values.RemoveAt(i--);
            }
        }

        #region Connector

        void IConnector<ITargetProvider>.Connect(ITargetProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            Assert.IsNull(m_TargetProvider);
            m_TargetProvider = t;
        }
        void IConnector<ITargetProvider>.Disconnect(ITargetProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            Assert.IsTrue(ReferenceEquals(m_TargetProvider, t));
            m_TargetProvider = null;
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            Assert.IsNull(m_DataProvider);
            m_DataProvider = t;
        }
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));

            Assert.IsTrue(ReferenceEquals(m_DataProvider, t));
            m_DataProvider = null;
        }

        void IConnector<IActorViewProvider>.Connect(IActorViewProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));
            m_ViewProvider = t;
        }
        void IConnector<IActorViewProvider>.Disconnect(IActorViewProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));
            m_ViewProvider = null;
        }

        #endregion
    }
}