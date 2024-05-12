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
// File created : 2024, 05, 07 23:05

#endregion

using System;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    internal readonly struct RuntimePassive : IComparable<RuntimePassive>
    {
        public readonly string             id;
        public readonly int                type, level;
        public readonly PassiveSheet.AffectType affectType;

        public readonly Condition activateCondition;
        public readonly string    activateValue;

        public readonly Condition executeCondition;
        public readonly string    executeValue;
        public readonly float     executeProbability;

        public readonly PassiveSheet.Target         conclusionTarget;
        public readonly PassiveSheet.Position         conclusionPosition;
        public readonly PassiveSheet.ConclusionType conclusionType;
        public readonly DynamicReference       conclusionValue;

        public RuntimePassive(PassiveSheet.Row data)
        {
            id         = data.Id;
            type       = data.Definition.Type;
            level      = data.Definition.Level;
            affectType = data.Definition.AffectType;

            activateCondition = data.Activation.Condition;
            activateValue     = data.Activation.Value;

            executeCondition   = data.Execution.Condition;
            executeValue       = data.Execution.Value;
            executeProbability = data.Execution.Probability;

            conclusionTarget   = data.Conclusion.Target;
            conclusionPosition = data.Conclusion.Position;
            conclusionType     = data.Conclusion.Type;
            conclusionValue    = data.Conclusion.Value;
        }

        public int CompareTo(RuntimePassive other)
        {
            if (type < other.type) return -1;
            return type > other.type ? 1 : 0;
        }
    }
}