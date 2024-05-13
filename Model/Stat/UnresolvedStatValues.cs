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
// File created : 2024, 05, 11 15:05

#endregion

using System.Collections;
using System.Collections.Generic;
using Cathei.BakingSheet;
using Newtonsoft.Json;
using UnityEngine.Assertions;

namespace Vvr.Model.Stat
{
    /// <summary>
    /// Unresolved version of <see cref="StatValues"/>
    /// </summary>
    /// <remarks>
    /// This class designed for parsing sheet that should be indexed by system.
    /// </remarks>
    [JsonConverter(typeof(UnresolvedStatValuesJsonConverter))]
    class UnresolvedStatValues : UnresolvedValues<StatValues>, IReadOnlyStatValues
    {
        protected override StatValues Resolve(ISheet s)
        {
            StatSheet sheet = s as StatSheet;
            Assert.IsNotNull(sheet, "sheet != null");

            float[] v = new float[values.Length];

            int  i     = 0;
            long query = 0;
            foreach (var item in ids)
            {
                int index = sheet[item].Index;

                long e = 1L << index;
                query |= e;

                v[i] = values[i];
                i++;
            }

            return new StatValues((StatType)query, v);
        }

        IEnumerator<KeyValuePair<StatType, float>> IEnumerable<KeyValuePair<StatType, float>>.GetEnumerator() => Value.GetEnumerator();
        IEnumerator IEnumerable.                                                              GetEnumerator() => Value.GetEnumerator();

        float IReadOnlyStatValues.this[StatType t] => Value[t];

        StatType IReadOnlyStatValues.            Types  => Value.Types;
        IReadOnlyList<float> IReadOnlyStatValues.Values => (IReadOnlyList<float>)Value.Values;
    }
}