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
using UnityEngine;
using Vvr.Controller.Condition;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.UI.Observer;

namespace Vvr.Controller.Abnormal
{
    partial class AbnormalController : ITimeUpdate
    {
         private bool CheckTimeCondition(Value value)
         {
             if (value.abnormal.timeCondition       == null ||
                 value.abnormal.timeCondition.Count == 0) return true;

            bool            prevResult = false;
            LogicalOperator op         = default;
            for (int i = 0; i < value.abnormal.timeCondition?.Count; i++)
            {
                Model.Condition condition = value.abnormal.timeCondition[i];
                if (Enum.IsDefined(VvrTypeHelper.TypeOf<LogicalCondition>.Type, (LogicalCondition)condition))
                {
                    op |= (LogicalCondition)condition;
                    if (i + 1 < value.abnormal.timeCondition.Count)
                    {
                        bool result = Owner.ConditionResolver[value.abnormal.timeCondition[++i]](null);

                        switch ((LogicalCondition)condition)
                        {
                            case LogicalCondition.AND:
                                prevResult = prevResult && result;
                                break;
                            case LogicalCondition.None:
                            case LogicalCondition.OR:
                                prevResult = prevResult || result;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                else prevResult |= Owner.ConditionResolver[condition](null);
            }
            $"[Abnormal:{Owner.GetInstanceID()}] Processing time condition: {op} = {prevResult}".ToLog();
            return prevResult;
        }
        async UniTask ITimeUpdate.OnEndUpdateTime()
        {
        }
        async UniTask ITimeUpdate.OnUpdateTime(int currentTime, int deltaTime)
        {
            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Abnormal);

            bool shouldUpdate = false;
            for (int i = 0; i < m_Values.Count; i++)
            {
                Value e = m_Values[i];
                // This is ordered list. infinite duration should be last of the list
                if (e.abnormal.duration < 0) break;

                if (!CheckTimeCondition(e)) continue;

                float delayDuration = e.delayDuration -= deltaTime;
                m_Values[i] = e;

                // If abnormal is delayed execution
                if (0 < e.abnormal.delayTime)
                {
                    // Check has passed delay time
                    if (delayDuration <= 0) continue;

                    // Check should execute
                    if (e.updateCount < 1)
                    {
#if UNITY_EDITOR
                        $"[Abnormal:{Owner.GetInstanceID()}] Delayed abnormal activation {e.abnormal.hash.Key}".ToLog();
#endif
                        e.updateCount   = 1;
                        m_Values[i]  = e;

                        shouldUpdate = true;
                    }
                }

                if (e.abnormal.isInfiniteDuration != 1)
                {
                    float duration = e.duration -= deltaTime;
                    // Abnormal completed
                    if (duration <= 0)
                    {
                        // Sub stack and update time to current
                        e.stack--;
                        e.duration  = e.abnormal.duration;
                        m_Values[i] = e;

                        // If abnormal has no stack, should remove
                        if (e.stack <= 0)
                        {
#if UNITY_EDITOR
                            $"[Abnormal:{Owner.GetInstanceID()}] Remove {e.abnormal.hash.Key}".ToLog();
#endif
                            ObjectObserver<AbnormalController>.ChangedEvent(this);
                            ObjectObserver<IStatValueStack>.ChangedEvent(Owner.Stats);
                            m_Values.RemoveAt(i--);
                            m_IsDirty = true;

                            await trigger.Execute(Model.Condition.OnAbnormalRemoved, e.abnormal.id);
                            continue;
                        }
                    }
                }

                // Check max update count.
                if (e.abnormal.updateMaxCount < e.updateCount) continue;

                // Update logic
                // Only update when condition is Always.
                // Always condition takes interval as time
                if (e.abnormal.enableUpdate &&
                    e.abnormal.updateCondition == 0)
                {
                    int updateCount
                        = Mathf.FloorToInt((currentTime - e.lastUpdatedTime) / e.abnormal.updateInterval);
                    for (int j = 0; j < updateCount; j++)
                    {
#if UNITY_EDITOR
                        $"[Abnormal:{Owner.GetInstanceID()}] Update({e.abnormal.updateCondition}) {e.abnormal.hash.Key}".ToLog();
#endif
                        e.updateCount++;
                        shouldUpdate = true;
                    }

                    e.lastUpdatedTime = currentTime;
                    m_Values[i]       = e;

                    if (updateCount > 0)
                        await trigger.Execute(Model.Condition.OnAbnormalUpdate, e.abnormal.id);
                }
            }

            if (shouldUpdate)
            {
                m_IsDirty = true;
                ObjectObserver<AbnormalController>.ChangedEvent(this);
                ObjectObserver<IStatValueStack>.ChangedEvent(Owner.Stats);
            }
        }
    }
}