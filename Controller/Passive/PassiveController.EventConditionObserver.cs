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
// File created : 2024, 05, 07 21:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    partial class PassiveController : IConditionObserver
    {
        ConditionQuery IConditionObserver.Filter => ConditionQuery.All;
        async UniTask IConditionObserver.OnExecute(Condition c, string value)
        {
            EventCondition condition = (EventCondition)c;
            for (int i = 0; i < m_Values.Count; i++)
            {
                var e = m_Values[i];

                await ExecuteValue(e, condition, value);
            }
        }

        private async UniTask ExecuteValue(Value e, EventCondition condition, string value)
        {
            // If condition is not met
            if (e.passive.activateCondition != (Condition)condition ||
                !Owner.ConditionResolver[e.passive.activateCondition](e.passive.activateValue))
            {
                return;
            }

            // Check can be execute
            // if (!Owner.ConditionResolver[e.passive.executeCondition](e.passive.executeValue))
            // {
            //     return;
            // }

            // Check probability
            if (!ProbabilityResolver.Get().Resolve(e.passive.executeProbability))
            {
                return;
            }

            var targetDef = new TargetDefinition(
                e.passive.conclusionTarget, e.passive.conclusionPosition
            );

            switch (e.passive.conclusionType)
            {
                case PassiveSheet.ConclusionType.Skill:
                    SkillSheet.Row skill = e.passive.conclusionValue.Reference as SkillSheet.Row;
                    if (skill == null)
                    {
                        "err, skill not found".ToLogError();
                        return;
                    }

                    foreach (var target in m_TargetProvider.FindTargets(Owner, targetDef))
                    {
                        if (!target.ConditionResolver[e.passive.executeCondition](e.passive.executeValue)) continue;

                        await Owner.Skill.Queue(skill, target);
                    }

                    break;
                case PassiveSheet.ConclusionType.Abnormal:
                    AbnormalSheet.Row abnormal = e.passive.conclusionValue.Reference as AbnormalSheet.Row;
                    if (abnormal == null)
                    {
                        "err, abnormal not found".ToLogError();
                        return;
                    }

                    foreach (var target in m_TargetProvider.FindTargets(Owner, targetDef))
                    {
                        if (!target.ConditionResolver[e.passive.executeCondition](e.passive.executeValue)) continue;

                        await target.Abnormal.Add(abnormal);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}