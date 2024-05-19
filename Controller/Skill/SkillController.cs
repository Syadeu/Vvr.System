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
using System.Linq;
using Cathei.BakingSheet;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Controller.Skill
{
    public sealed partial class SkillController : ITimeUpdate, IDisposable, ISkill
    {
        struct Value : ITargetDefinition
        {
            public readonly Hash           hash;
            public readonly SkillSheet.Row skill;

            public float delayedExecutionTime;

            public readonly IActor overrideTarget;

            public Value(SkillSheet.Row d, IActor ta)
            {
                hash                 = new Hash(d.Id);
                skill                 = d;
                delayedExecutionTime = d.Definition.Delay;
                overrideTarget       = ta;
            }

            public SkillSheet.Target   Target   => skill.Definition.Target;
            public SkillSheet.Position Position => skill.Definition.Position;
        }

        private IEventViewProvider m_ViewProvider;
        private IActorDataProvider m_DataProvider;
        private ITargetProvider    m_TargetProvider;

        private readonly List<Value> m_Values = new();

        private readonly List<Hash>              m_SkillCooltimeKeys = new();
        private readonly Dictionary<Hash, float> m_SkillCooltimes    = new();

        private IActor Owner { get; }
        private bool Disposed { get; set; }

        public SkillController(IActor o)
        {
            Owner = o;
        }
        public void Dispose()
        {
            Clear();

            m_DataProvider   = null;
            m_TargetProvider = null;

            Disposed = true;
        }

        public void Clear()
        {
            m_SkillCooltimeKeys.Clear();
            m_SkillCooltimes.Clear();
            m_Values.Clear();
        }

        float ISkill.GetSkillCooltime(ISkillID skill)
        {
            Hash hash = new Hash(skill.Id);
            m_SkillCooltimes.TryGetValue(hash, out float v);
            return v;
        }
        int ISkill.GetSkillCount()
        {
            var data         = m_DataProvider.Resolve(Owner.Id);
            return data.Skills.Count(x => x != null);
        }

        IUniTaskAsyncEnumerable<ISkillData> ISkill.GetSkills()
        {
            return UniTaskAsyncEnumerable.Create<ISkillData>(async (wr, token) =>
            {
                var data = m_DataProvider.Resolve(Owner.Id);

                foreach (var skill in data.Skills)
                {
                    if (skill != null)
                    {
                        await wr.YieldAsync(skill);
                    }
                }
            });
        }
        async UniTask ISkill.Queue(int index)
        {
            var data = m_DataProvider.Resolve(Owner.Id);
            await Queue(data.Skills[index]);
        }
        public async UniTask Queue(ISkillID skill)
        {
            SkillSheet.Row skillRow;
            if (skill is SkillSheet.Row row) skillRow = row;
            else
            {
                var skillData = m_DataProvider.Resolve(Owner.Id);
                skillRow = skillData.Skills.FirstOrDefault(x => x.Id == skill.Id);
            }

            if (skillRow != null)
            {
                await Queue(skillRow, null);
            }
        }

        public async UniTask Queue(ISkillID data, IActor specifiedTarget)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));
            Assert.IsFalse(m_Values.Any(x=>x.skill == data),
                $"Skill {data.Id} already queued but trying to queue another one. {m_Values.Count}");

            $"[Skill:{Owner.DisplayName}]: SKILL QUEUED {data.Id}, {m_Values.Count}".ToLog();

            SkillSheet.Row skillRow;
            if (data is SkillSheet.Row row) skillRow = row;
            else
            {
                var skillData = m_DataProvider.Resolve(Owner.Id);
                skillRow = skillData.Skills.FirstOrDefault(x => x.Id == data.Id);
            }

            var value = new Value(skillRow, specifiedTarget);

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Skill);
            await ExecuteSkill(trigger, value);
        }

        async UniTask ExecuteSkill(ConditionTrigger trigger, Value value)
        {
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            // Check cooltime
            if (m_SkillCooltimes.ContainsKey(value.hash))
            {
                $"[Skill:{Owner.DisplayName}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) is in cooltime {m_SkillCooltimes[value.hash]}"
                    .ToLog();
                return;
            }

            $"[Skill:{Owner.DisplayName}:{Owner.GetInstanceID()}] Skill start {value.skill.Id}".ToLog();


            await trigger.Execute(Model.Condition.OnSkillStart, value.skill.Id);

            #region Warmup

            var viewTarget       = await m_ViewProvider.Resolve(Owner);
            var skillEventHandle = viewTarget.GetComponent<ISkillEventHandler>();
            if (skillEventHandle != null)
            {
                SkillEffectEmitter emitter = new SkillEffectEmitter(value.skill.Presentation.SelfEffect);
                await skillEventHandle
                    .OnSkillStart(emitter)
                    .SuppressCancellationThrow()
                    .AttachExternalCancellation(viewTarget.GetCancellationTokenOnDestroy())
                    ;
            }
            else if (value.skill.Presentation.SelfEffect.IsValid())
            {
                Transform view = await m_ViewProvider.Resolve(Owner);

                var effectPool = GameObjectPool.Get(value.skill.Presentation.SelfEffect);
                var effect = await effectPool.SpawnEffect(
                    view.position, Quaternion.identity);
                while (!effect.Reserved)
                {
                    await UniTask.Yield();
                }
            }

            #endregion

            // Check value can be executed immediately
            if (value.delayedExecutionTime > 0)
            {
                m_Values.Add(value);
                return;
            }

            if (skillEventHandle != null)
            {
                // SkillEffectEmitter emitter = new SkillEffectEmitter(value.skill.Presentation.SelfEffect);
                await skillEventHandle
                        .OnSkillCasting(null)
                        .SuppressCancellationThrow()
                        .AttachExternalCancellation(viewTarget.GetCancellationTokenOnDestroy())
                    ;
            }

            await ExecuteSkillBody(trigger, value);
        }

        async UniTask ExecuteSkillBody(ConditionTrigger trigger, Value value)
        {
            Assert.IsFalse(Owner.Disposed);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            if (value.overrideTarget != null)
            {
                await ExecuteSkillTarget(trigger, value, value.overrideTarget);
            }
            else
            {
                int count          = 0;
                foreach (var target in m_TargetProvider.FindTargets(Owner, value))
                {
                    // If target count exceed its capability
                    if (value.skill.Definition.TargetCount <= count++) break;

                    await ExecuteSkillTarget(trigger, value, target);
                }
            }

            RegisterCooltime(value);
            $"[Skill:{Owner.DisplayName}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) cooltime: {m_SkillCooltimes[value.hash]}".ToLog();
        }

        async UniTask ExecuteSkillTarget(ConditionTrigger trigger, Value value, IActor target)
        {
            Assert.IsFalse(target.Disposed);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            #region Warmup

            Transform targetView = await m_ViewProvider.Resolve(target);
            Transform view       = await m_ViewProvider.Resolve(Owner);

            var skillEventHandle = view.GetComponent<ISkillEventHandler>();
            if (skillEventHandle != null)
            {
                SkillEffectEmitter emitter = new SkillEffectEmitter(value.skill.Presentation.TargetEffect);
                await skillEventHandle
                        .OnSkillEnd(targetView, emitter)
                        .SuppressCancellationThrow()
                        .AttachExternalCancellation(view.GetCancellationTokenOnDestroy())
                    ;
            }
            else if (value.skill.Presentation.TargetEffect.IsValid())
            {
                var effectPool = GameObjectPool.Get(value.skill.Presentation.TargetEffect);
                var effect = await effectPool.SpawnEffect(
                    targetView.position, Quaternion.identity);
                while (!effect.Reserved)
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

                    await target.Abnormal.Add(e);
                }

                float dmg = Owner.Stats[StatType.ATT] * value.skill.Execution.Multiplier;
                switch (value.skill.Execution.Method)
                {
                    case SkillSheet.Method.Damage:
                        target.Stats.Push<DamageProcessor>(
                            value.skill.Execution.TargetStat.Ref.ToStat(), dmg);
                        await targetTrigger.Execute(Model.Condition.OnHit, null);

                        break;
                    case SkillSheet.Method.Default:
                    default:
                        target.Stats.Push(
                            value.skill.Execution.TargetStat.Ref.ToStat(), dmg);

                        await targetTrigger.Execute(Model.Condition.OnHit, null);
                        break;
                }
            }

            #endregion

            await trigger.Execute(Model.Condition.OnSkillEnd, value.skill.Id);

            $"[Skill:{Owner.Owner}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) executed to {target.GetInstanceID()}({target.Owner})".ToLog();
        }

        private void RegisterCooltime(in Value value)
        {
            Assert.IsFalse(m_SkillCooltimes.ContainsKey(value.hash));

            m_SkillCooltimes[value.hash] = value.skill.Definition.Cooltime;
            m_SkillCooltimeKeys.Add(value.hash);
        }

        async UniTask ITimeUpdate.OnUpdateTime(int currentTime, int deltaTime)
        {
            for (int i = m_SkillCooltimeKeys.Count - 1; i >= 0; i--)
            {
                var key = m_SkillCooltimeKeys[i];
                if (!m_SkillCooltimes.ContainsKey(key))
                {
                    // XXX: ???
                    $"Skill cooltime not found but has keys in: somethings went wrong {key}".ToLog();
                    m_SkillCooltimeKeys.RemoveAt(i);
                    continue;
                }

                float duration = m_SkillCooltimes[key] - deltaTime;
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

                await trigger.Execute(Model.Condition.OnSkillCasting, e.skill.Id);

                Transform viewTarget       = await m_ViewProvider.Resolve(Owner);
                var       skillEventHandle = viewTarget.GetComponent<ISkillEventHandler>();
                if (skillEventHandle != null)
                {
                    // SkillEffectEmitter emitter = new SkillEffectEmitter(value.skill.Presentation.SelfEffect);
                    await skillEventHandle
                            .OnSkillCasting(null)
                            .SuppressCancellationThrow()
                            .AttachExternalCancellation(viewTarget.GetCancellationTokenOnDestroy())
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

        void IConnector<IEventViewProvider>.Connect(IEventViewProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));
            m_ViewProvider = t;
        }
        void IConnector<IEventViewProvider>.Disconnect(IEventViewProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(SkillController));
            m_ViewProvider = null;
        }
    }
}