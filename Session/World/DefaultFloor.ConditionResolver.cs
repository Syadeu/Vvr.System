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

using System.Linq;
using Vvr.Controller.Condition;

namespace Vvr.Session.World
{
    partial class DefaultFloor
    {
        private bool Started { get; set; }

        protected override void Register(ConditionResolver conditionResolver)
        {
            conditionResolver[Model.Condition.OnFloorStarted] = x => ConditionTrigger.Any(this, Model.Condition.OnFloorStarted, x);
            conditionResolver[Model.Condition.OnFloorEnded] = x => ConditionTrigger.Any(this, Model.Condition.OnFloorEnded, x);
            conditionResolver[Model.Condition.OnStageStarted] = x => Started && ConditionTrigger.Any(this, Model.Condition.OnStageStarted, x);
            conditionResolver[Model.Condition.OnStageEnded] = x => Started && ConditionTrigger.Any(this, Model.Condition.OnStageEnded, x);

            conditionResolver[Model.Condition.IsFloorStarted] = x => Started  && Data.stages.First().Id == x;
            conditionResolver[Model.Condition.IsFloorEnded]   = x => !Started && Data.stages.First().Id == x;
            conditionResolver[Model.Condition.IsStageStarted] = x => Started  && m_CurrentStage != null && m_CurrentStage.Data.stage.Id == x;
            conditionResolver[Model.Condition.IsStageEnded] = x => Started  && m_CurrentStage != null && m_CurrentStage.Data.stage.Id == x;
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