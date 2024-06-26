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
// File created : 2024, 06, 17 22:06

#endregion

using Vvr.Controller.Stat;
using Vvr.Model.Stat;

namespace Vvr.Controller.Tests
{
    class TestStatModifier : IStatModifier
    {
        public bool IsDirty { get; set; }
        public int  Order   { get; set; }

        public IReadOnlyStatValues TargetStatValues { get; set; }

        public TestStatModifier(IReadOnlyStatValues targetStatValues)
        {
            TargetStatValues = targetStatValues;
        }

        void IStatModifier.UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
        {
            stats += TargetStatValues;
        }
    }
}