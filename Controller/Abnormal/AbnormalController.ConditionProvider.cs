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

using System;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Abnormal
{
    partial class AbnormalController : IAbnormalConditionProvider
    {
        bool IAbnormalConditionProvider.Resolve(AbnormalCondition condition, string value)
        {
            bool result;
            switch (condition)
            {
                case AbnormalCondition.HasAbnormal:
                    result = Contains(new Hash(value));
                    break;
                case AbnormalCondition.HasNotAbnormal:
                    result = !Contains(new Hash(value));
                    break;
                case AbnormalCondition.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }

            $"[Condition:Abnormal] Resolved {VvrTypeHelper.Enum<AbnormalCondition>.ToString(condition)}: {result}".ToLog();
            return result;
        }
    }
}