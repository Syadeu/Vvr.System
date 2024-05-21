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
// File created : 2024, 05, 07 02:05

#endregion

using Vvr.Model.Stat;

namespace Vvr.Controller.Stat
{
    public struct DamageProcessor : IStatValueProcessor
    {
        public float Process(in IReadOnlyStatValues stats, float value)
        {
            float defMultiplier = 0.01f;

            float def = stats[StatType.DEF] + stats[StatType.ARM];
            // if (def == 0) return dmg;

            float lvl = 1;
            // if (m_Parent.Data.TryGetAttribute(out ActorLevelAttribute l))
            // {
            //     lvl = l.Level + 1;
            // }

            float pa;
            if (def >= 0)
            {
                pa = 1 / (1 + def * defMultiplier * lvl);
            }
            else
            {
                pa = 2 - 1 / (1 - def * defMultiplier * lvl);
            }

            value *= pa;
            return -value;
        }
    }
}