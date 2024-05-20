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
using System.Buffers;
using System.Linq;
using Vvr.Controller.Condition;
using Vvr.Model;

namespace Vvr.Session.World
{
    partial class DefaultFloor
    {
        private bool WasStartedOnce { get; set; }
        private bool Started        { get; set; }

        protected override void Register(ConditionResolver conditionResolver)
        {
            conditionResolver[Model.Condition.OnFloorStarted] = x => ConditionTrigger.Any(this, Model.Condition.OnFloorStarted, x);
            conditionResolver[Model.Condition.OnFloorEnded] = x => ConditionTrigger.Any(this, Model.Condition.OnFloorEnded, x);
            conditionResolver[Model.Condition.OnStageStarted] = x => Started && ConditionTrigger.Any(this, Model.Condition.OnStageStarted, x);
            conditionResolver[Model.Condition.OnStageEnded] = x => Started && ConditionTrigger.Any(this, Model.Condition.OnStageEnded, x);

            conditionResolver[Model.Condition.IsFloorStarted] = x =>
            {
                if (!Started || !Data.stages.Any()) return false;

                if (x.IsNullOrEmpty() || !int.TryParse(x, out var st)) return true;

                var stage = Data.stages.First();
                return stage.Floor == st;
            };
            conditionResolver[Model.Condition.IsFloorEnded]   = x =>
            {
                if (!Data.stages.Any()) return true;

                if (!Started && !WasStartedOnce) return false;

                if (x.IsNullOrEmpty() || !int.TryParse(x, out var st)) return true;

                var stage = Data.stages.First();
                return stage.Floor == st;
            };
            conditionResolver[Model.Condition.IsStageStarted] = x =>
            {
                if (!Started || !WasStartedOnce || !Data.stages.Any()) return false;

                if (x.IsNullOrEmpty()) return true;

                if (!TryGetStageElementIndex(x, out int index)) return false;

                return index <= m_CurrentStageIndex;
            };
            conditionResolver[Model.Condition.IsStageEnded] = x =>
            {
                if (!Started || !WasStartedOnce || !Data.stages.Any()) return false;

                if (x.IsNullOrEmpty()) return true;

                if (!TryGetStageElementIndex(x, out int index)) return false;

                return index < m_CurrentStageIndex;
            };
        }

        private bool TryGetStageElementIndex(string id, out int i)
        {
            i = 0;
            foreach (var stage in Data.stages)
            {
                if (stage.Id == id) return true;
                i++;
            }

            i = -1;
            return false;
        }
    }
}