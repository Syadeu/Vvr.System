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
// File created : 2024, 06, 27 01:06

#endregion

using JetBrains.Annotations;
using Vvr.Model.Stat;

namespace Vvr.Controller.Stat
{
    [PublicAPI]
    public sealed class HpShieldPostProcessor : IStatPostProcessor
    {
        public static IStatPostProcessor Static { get; } = new HpShieldPostProcessor();

        public int Order => StatModifierOrder.PostProcess;

        public void OnChanged(in StatValues previous, ref StatValues current)
        {
            float prevHp    = previous[StatType.HP];
            float currentHp = current[StatType.HP];

            float shield = current[StatType.SHD];
            // float sub    = currentHp - prevHp;

            if (currentHp < prevHp && shield > 0)
            {
                // $"prev: {previous}\ncurrent: {current}".ToLog();

                float sub = prevHp - currentHp;
                if (sub > shield)
                {
                    currentHp             -= sub - shield;
                    current[StatType.HP]  =  currentHp;
                    current[StatType.SHD] =  0;

                    // $"11. {current[StatType.HP]}, {current[StatType.SHD]}".ToLog();
                }
                else
                {
                    current[StatType.SHD] = shield - sub;
                    current[StatType.HP]  = prevHp;

                    // $"22. {current[StatType.HP]}, {current[StatType.SHD]}".ToLog();
                }
            }
        }
    }
}