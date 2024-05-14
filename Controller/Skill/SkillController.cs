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
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Skill
{
    public sealed partial class SkillController : ITimeUpdate, IDisposable, ISkill
    {
        public static SkillController Create(IActor o)
        {
            return new SkillController(o);
        }

        struct SkillData
        {
            public readonly SkillSheet.Row data;
        }
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

        private AsyncLazy<IActorDataProvider> m_DataProvider;
        private AsyncLazy<ITargetProvider>    m_TargetProvider;

        private readonly List<Value> m_Values = new();

        private readonly List<Hash>              m_SkillCooltimeKeys = new();
        private readonly Dictionary<Hash, float> m_SkillCooltimes    = new();

        private IActor Owner { get; }

        private SkillController(IActor o)
        {
            Owner = o;

            m_DataProvider   = MPC.Provider.Provider.Static.GetLazyAsync<IActorDataProvider>();
            m_TargetProvider = MPC.Provider.Provider.Static.GetLazyAsync<ITargetProvider>();
        }
        public void Dispose()
        {
            Clear();

            m_DataProvider   = null;
            m_TargetProvider = null;
        }

        public void Clear()
        {
            m_SkillCooltimeKeys.Clear();
            m_SkillCooltimes.Clear();
            m_Values.Clear();
        }

        public async UniTask<int> GetSkillCount()
        {
            var dataProvider = await m_DataProvider;
            var data         = dataProvider.Resolve(Owner.DataID);
            return data.Skills.Count(x => x.Ref != null);
        }
        public async UniTask Queue(int index)
        {
            var dataProvider = await m_DataProvider;
            var data = dataProvider.Resolve(Owner.DataID);
            await Queue(data.Skills[index].Ref);
        }

        public async UniTask Queue(SkillSheet.Row data) => await Queue(data, null);
        public async UniTask Queue(SkillSheet.Row data, IActor specifiedTarget)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            $"{Owner.Owner}:{Owner.GetInstanceID()} SKILL QUEUED {data.Id}".ToLog();

            var value = new Value(data, specifiedTarget);

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Skill);
            await ExecuteSkill(trigger, value);
        }

        async UniTask ExecuteSkill(ConditionTrigger trigger, Value value)
        {
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            // Check cooltime
            if (m_SkillCooltimes.ContainsKey(value.hash))
            {
                $"[Skill:{Owner.Owner}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) is in cooltime {m_SkillCooltimes[value.hash]}"
                    .ToLog();
                return;
            }

            $"[Skill:{Owner.Owner}:{Owner.GetInstanceID()}] Skill start {value.skill.Id}".ToLog();

            await trigger.Execute(Model.Condition.OnSkillStart, value.skill.Id);

            #region Warmup

            if (value.skill.Presentation.SelfEffect.IsValid())
            {
                IEventViewProvider viewProvider = await MPC.Provider.Provider.Static.GetAsync<IEventViewProvider>();
                Transform          view         = await viewProvider.Resolve(Owner);

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
                var targetProvider = await m_TargetProvider;
                foreach (var target in targetProvider.FindTargets(Owner, value))
                {
                    // If target count exceed its capability
                    if (value.skill.Definition.TargetCount <= count++) break;

                    await ExecuteSkillTarget(trigger, value, target);
                }
            }

            RegisterCooltime(value);
            $"[Skill:{Owner.Owner}:{Owner.GetInstanceID()}] Skill({value.skill.Id}) cooltime: {m_SkillCooltimes[value.hash]}".ToLog();
        }

        async UniTask ExecuteSkillTarget(ConditionTrigger trigger, Value value, IActor target)
        {
            Assert.IsFalse(target.Disposed);
            Assert.IsTrue(Owner.ConditionResolver[Model.Condition.IsActorTurn](null));

            // Cache effect position
            // because target can be destroyed during this skill execution (ex. actor is dead)
            IEventViewProvider viewProvider = await MPC.Provider.Provider.Static.GetAsync<IEventViewProvider>();
            Vector3            viewPosition;
            {
                Transform view = await viewProvider.Resolve(target);
                viewPosition = view.position;
            }

            #region Execution body

            using (var targetTrigger = ConditionTrigger.Push(target, ConditionTrigger.Skill))
            {
                for (int i = 0; i < value.skill.Abnormal?.Count; i++)
                {
                    AbnormalSheet.Row e = value.skill.Abnormal[i].Ref;

                    await target.Abnormal.Add(e);
                }

                float   dmg = Owner.Stats[StatType.ATT] * value.skill.Execution.Multiplier;
                switch (value.skill.Execution.Method)
                {
                    case SkillSheet.Method.Damage:
                        target.Stats.Push<DamageProcessor>(
                            value.skill.Execution.TargetStat.Ref.ToStat(), dmg);
                        await targetTrigger.Execute(Model.Condition.OnHit, $"{dmg}");

                        break;
                    case SkillSheet.Method.Default:
                    default:
                        dmg *= value.skill.Execution.Multiplier;
                        target.Stats.Push(
                            value.skill.Execution.TargetStat.Ref.ToStat(), dmg);

                        await targetTrigger.Execute(Model.Condition.OnHit, $"{dmg}");
                        break;
                }
            }

            #endregion

            if (value.skill.Presentation.TargetEffect.IsValid())
            {
                var effectPool = GameObjectPool.Get(value.skill.Presentation.TargetEffect);
                var effect = await effectPool.SpawnEffect(
                    viewPosition, Quaternion.identity);
                while (!effect.Reserved)
                {
                    await UniTask.Yield();
                }

                // "play hit eff".ToLog();
            }

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
                $"22. not this actor turn. skip {Owner.DisplayName}".ToLog();
                return;
            }

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Skill);

            for (int i = 0; i < m_Values.Count; i++)
            {
                Value e = m_Values[i];

                await trigger.Execute(Model.Condition.OnSkillCasting, e.skill.Id);

                // If time completed, execute
                if (e.delayedExecutionTime > 0) continue;

                await ExecuteSkillBody(trigger, e);

                m_Values.RemoveAt(i--);
            }
        }
    }
}