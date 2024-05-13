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
// File created : 2024, 05, 12 00:05

#endregion

using System;
using System.Collections.Generic;

namespace Vvr.Controller.Stat
{
    struct StatModifierComparer : IComparer<IStatModifier>
    {
        public static readonly IComparer<IStatModifier>           Static   = default(StatModifierComparer);
        public static readonly Func<IStatModifier, IStatModifier> Selector = x => x;

        public int Compare(IStatModifier x, IStatModifier y)
        {
            if (x == null) return y == null ? 0 : 1;
            if (y == null) return -1;

            if (x.Order < y.Order) return -1;
            return x.Order > y.Order ? 1 : 0;
        }
    }
}