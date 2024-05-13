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
// File created : 2024, 05, 13 14:05
#endregion

using System;
using Cysharp.Threading.Tasks;
using Vvr.MPC.Provider;
using Vvr.System.Model;
using NotImplementedException = System.NotImplementedException;

namespace Vvr.System.Controller
{
    partial class DefaultFloor
    {
        private bool Started { get; set; }

        protected virtual partial void Connect(ConditionResolver conditionResolver)
        {
            conditionResolver[Condition.OnFloorStarted] = x => ConditionTrigger.Any(this, Condition.OnFloorStarted, x);
            conditionResolver[Condition.OnFloorEnded] = x => ConditionTrigger.Any(this, Condition.OnFloorEnded, x);
            conditionResolver[Condition.OnStageStarted] = x => Started && ConditionTrigger.Any(this, Condition.OnStageStarted, x);
            conditionResolver[Condition.OnStageEnded] = x => Started && ConditionTrigger.Any(this, Condition.OnStageEnded, x);

            conditionResolver[Condition.IsFloorStarted] = x => Started  && Data.stages.First.Value.Id == x;
            conditionResolver[Condition.IsFloorEnded]   = x => !Started && Data.stages.First.Value.Id == x;
            conditionResolver[Condition.IsStageStarted] = x => Started  && m_CurrentStage != null && m_CurrentStage.Data.stageId == x;
            conditionResolver[Condition.IsStageEnded] = x => Started  && m_CurrentStage != null && m_CurrentStage.Data.stageId == x;
        }

        // ConditionQuery IConditionObserver.Filter { get; } =
        //     Condition.OnFloorStarted | (ConditionQuery)Condition.OnFloorEnded |
        //     (ConditionQuery)Condition.OnStageStarted | (ConditionQuery)Condition.OnStageEnded;
        //
        // private partial async UniTask OnEventExecuted(IEventTarget target, Condition condition, string value)
        // {
        //
        // }
    }
}