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
// File created : 2024, 05, 07 03:05

#endregion

using Cysharp.Threading.Tasks;
using Vvr.Controller.Condition;
using Vvr.Model;

namespace Vvr.Controller.Abnormal
{
    partial class AbnormalController : IConditionObserver
    {
        ConditionQuery IConditionObserver.Filter => ConditionQuery.All - Model.Condition.Always;

        async UniTask IConditionObserver.OnExecute(Model.Condition c, string value)
        {
            EventCondition condition = (EventCondition)c;

            // Always should not be checked
            if (condition == 0)
            {
                return;
            }

            bool shouldUpdate = false;
            for (int i = 0; i < m_Values.Count; i++)
            {
                Value e = m_Values[i];
                if (CheckCancellation(ref i, condition))
                {
                    continue;
                }

                // If has update and met condition
                if (e.abnormal.enableUpdate &&
                    e.abnormal.updateCondition == (Model.Condition)condition)
                {
                    if (Owner.ConditionResolver[e.abnormal.updateCondition](e.abnormal.updateValue))
                    {
#if UNITY_EDITOR
                        $"[Abnormal:{Owner.GetInstanceID()}] Update({e.abnormal.updateCondition}) {e.abnormal.hash.Key}".ToLog();
#endif
                        e.updateCount++;
                        m_Values[i]  = e;
                        shouldUpdate = true;
                    }
                }
            }

            if (shouldUpdate) m_IsDirty = true;
        }

        private bool CheckCancellation(ref int index, EventCondition condition)
        {
            Value e = m_Values[index];

            if (e.abnormal.cancelCondition != (Model.Condition)condition) return false;

            // Check probability
            if (!ProbabilityResolver.Get().Resolve(e.abnormal.cancelProbability))
            {
                return false;
            }

            if (!Owner.ConditionResolver[e.abnormal.cancelCondition](e.abnormal.cancelValue))
            {
                return false;
            }

            // Cancel abnormal
            if (e.abnormal.cancelClearAllStacks)
            {
                e.stack = 0;
            }
            else e.stack--;

            if (e.stack <= 0)
            {
#if UNITY_EDITOR
                $"[Abnormal:{Owner.GetInstanceID()}] Canceled({e.abnormal.updateCondition}) {e.abnormal.hash.Key}".ToLog();
#endif
                m_Values.RemoveAt(index--);
                return true;
            }

            m_Values[index] = e;
            return false;
        }
    }
}