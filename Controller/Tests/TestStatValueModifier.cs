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
// File created : 2024, 06, 27 00:06

#endregion

using Vvr.Controller.Stat;
using Vvr.Model.Stat;

namespace Vvr.Controller.Tests
{
    class TestStatValueModifier : IStatModifier
    {
        public bool IsDirty { get; set; } = true;
        public int  Order   { get; set; }

        public StatType StatType { get; set; }
        public float    Value    { get; set; }

        public TestStatValueModifier(int order, StatType t, float v)
        {
            Order    = order;
            StatType = t;
            Value    = v;
        }

        public void UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
        {
            stats |= StatType;

            stats[StatType] += Value;

            IsDirty = false;
        }
    }
    class TestStatValueMulModifier : IStatModifier
    {
        public bool IsDirty { get; set; } = true;
        public int  Order   { get; set; }

        public StatType StatType { get; set; }
        public float    Value    { get; set; }

        public TestStatValueMulModifier(int order, StatType t, float v)
        {
            Order    = order;
            StatType = t;
            Value    = v;
        }

        public void UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
        {
            stats |= StatType;

            stats[StatType] *= Value;

            IsDirty = false;
        }
    }
}