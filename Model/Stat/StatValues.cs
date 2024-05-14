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
// File created : 2024, 05, 05 16:05

#endregion

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Vvr.Model.Stat
{
    public delegate float StatValueGetterDelegate(in IReadOnlyStatValues stat);
    public delegate void StatValueSetterDelegate(in StatValues stat, float value);

    /// <summary>
    /// Represents a struct that holds a collection of Actor stat values.
    /// </summary>
    [SheetValueConverter(typeof(UnresolvedStatValuesConverter))]
    public readonly struct StatValues : IReadOnlyStatValues
    {
        private static readonly Dictionary<StatType, StatValueGetterDelegate>
            s_CachedGetter = new();
        private static readonly Dictionary<StatType, StatValueSetterDelegate>
            s_CachedSetter = new();

        /// <summary>
        /// Get delegate that returns value related StatType
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static StatValueGetterDelegate GetGetMethod(StatType t)
        {
            if (!s_CachedGetter.TryGetValue(t, out var d))
            {
                d                 = (in IReadOnlyStatValues x) => x[t];
                s_CachedGetter[t] = d;
            }
            return d;
        }
        /// <summary>
        /// Get delegate that returns value setter related StatType
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static StatValueSetterDelegate GetSetMethod(StatType t)
        {
            if (!s_CachedSetter.TryGetValue(t, out var d))
            {
                d              = (in StatValues x, float value) => x.SetValue(t, value);
                s_CachedSetter[t] = d;
            }

            return d;
        }

        /// <summary>
        /// Creates a new instance of StatValues with the specified StatType.
        /// </summary>
        /// <param name="t">The StatType to initialize the StatValues with. By default, it is set to ~0.</param>
        /// <returns>A new instance of StatValues.</returns>
        public static StatValues Create(StatType t = (StatType)~0)
        {
            return new StatValues(t, new float[t.Count()]);
        }

        public static StatValues Copy(IReadOnlyStatValues from)
        {
            return new StatValues(from.Types, from.Values.ToArray());
        }

        private readonly float[] m_Values;

        public float this[StatType t]
        {
            get => GetValue(t);
            set => SetValue(t, value);
        }

        /// <summary>
        /// Bit-masked StatType in this struct
        /// </summary>
        public StatType     Types  { get; }
        /// <summary>
        /// Returns all values
        /// </summary>
        public IList<float> Values => m_Values;

        IReadOnlyList<float> IReadOnlyStatValues.Values    => m_Values;

        internal StatValues(StatType query, float[] values)
        {
            Types  = query;
            m_Values = values;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified StatType in the StatValues collection.
        /// </summary>
        /// <param name="t">The StatType to locate in the StatValues collection.</param>
        /// <returns>
        /// The index of the first occurrence of the specified StatType, if found;
        /// otherwise, -1.
        /// </returns>
        [Pure, PublicAPI]
        public int IndexOf(StatType t)
        {
            long target  = (long)Types;
            long typeVal = (long)t;
            long e       = 1L;

            for (int i = 0, c = 0; i < 64 && c < m_Values.Length; i++, e <<= 1)
            {
                if ((target & e) != e) continue;
                if (typeVal      == e) return c;
                c++;
            }

            return -1;
        }

        /// <summary>
        /// Returns the value related to the specified StatType from the StatValues collection.
        /// </summary>
        /// <param name="t">The StatType to retrieve the value for.</param>
        /// <returns>The value related to the specified StatType, or 0 if the StatType is not found.</returns>
        [Pure, PublicAPI]
        public float GetValue(StatType t)
        {
            int index = IndexOf(t);
            if (index < 0) return 0;

            return Values[index];
        }

        /// <summary>
        /// Sets the value of a specific StatType in the StatValues struct.
        /// </summary>
        /// <param name="t">The StatType to set the value for.</param>
        /// <param name="v">The new value for the StatType.</param>
        [PublicAPI]
        public void SetValue(StatType t, float v)
        {
            int index = IndexOf(t);
            Assert.IsFalse(index < 0);

            Values[index] = v;
        }

        /// <summary>
        /// Clears all stat values to 0.
        /// </summary>
        [PublicAPI]
        public void Clear()
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                m_Values[i] = 0;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of StatType-statValue pairs in the StatValues object.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection of StatType-statValue pairs in the StatValues object.
        /// </returns>
        [PublicAPI]
        public IEnumerator<KeyValuePair<StatType, float>> GetEnumerator()
        {
            long target = (long)Types;
            for (int i = 0, c = 0; i < 64; i++)
            {
                long e = 1L << i;
                if ((target & e) != e) continue;

                yield return new((StatType)e, m_Values[c++]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var item in this)
            {
                sb.Append($"({item.Key}, {item.Value})");
            }
            return sb.ToString();
        }

        public static StatValues operator |(StatValues x, StatType t)
        {
            if ((x.Types & t) == t) return x;

            var result   = Create(x.Types | t);
            int maxIndex = result.Values.Count;
            for (int i = 0, c = 0, xx = 0; i < 64 && c < maxIndex; i++)
            {
                var e = 1L << i;
                if (((long)result.Types & e) == 0) continue;

                if (((long)x.Types & e) != 0) result.m_Values[c] = x.m_Values[xx++];
                c++;
            }
            return result;
        }
        public static StatValues operator +(StatValues x, IReadOnlyStatValues y)
        {
            if (y?.Values == null) return x;

            var newTypes = (x.Types | y.Types);
            var result   = (x.Types & y.Types) != y.Types ? Create(x.Types | y.Types) : x;

            int maxIndex = result.Values.Count;
            for (int i = 0, c = 0, xx = 0, yy = 0; i < 64 && c < maxIndex; i++)
            {
                var e = 1L << i;
                if (((long)newTypes & e) == 0) continue;

                if (((long)x.Types & e) != 0) result.m_Values[c] =  x.m_Values[xx++];
                if (((long)y.Types & e) != 0) result.m_Values[c] += y.Values[yy++];
                c++;
            }

            return result;
        }
        public static StatValues operator -(StatValues x, IReadOnlyStatValues y)
        {
            if (y?.Values == null) return x;

            var newTypes = (x.Types | y.Types);
            var result   = (x.Types & y.Types) != y.Types ? Create(x.Types | y.Types) : x;

            int maxIndex = result.Values.Count;
            for (int i = 0, c = 0, xx = 0, yy = 0; i < 64 && c < maxIndex; i++)
            {
                var e = 1L << i;
                if (((long)newTypes & e) == 0) continue;

                if (((long)x.Types & e) != 0) result.m_Values[c] =  x.m_Values[xx++];
                if (((long)y.Types & e) != 0) result.m_Values[c] -= y.Values[yy++];
                c++;
            }

            return result;
        }
    }
}