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

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;

namespace Vvr.Model
{
    /// <summary>
    /// 64 Condition query
    /// </summary>
    /// <remarks>
    /// This query can contain a maximum of 64 conditions.
    /// </remarks>
    public readonly struct ConditionQuery : IEquatable<ConditionQuery>, IEnumerable<Condition>
    {
        /// <summary>
        /// This helper property returns zero based bit flags.
        /// Might not hold all conditions if there's more than 64
        /// </summary>
        public static ConditionQuery All => new ConditionQuery(0, ~0L);

        private readonly short m_Offset;
        private readonly long  m_Filter;

        /// <summary>
        /// Counts all <see cref="Condition"/> in this query
        /// </summary>
        public int Count
        {
            get
            {
                long x     = m_Filter;
                int  count = 0;
                while (x != 0)
                {
                    count++;
                    x &= (x - 1);
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the maximum index of the conditions in the query.
        /// </summary>
        /// <remarks>
        /// This property returns the maximum index of the conditions in the query.
        /// The index corresponds to the position of the highest bit flag.
        /// If no conditions exist in the query, an InvalidOperationException is thrown.
        /// </remarks>
        public int MaxIndex
        {
            get
            {
                int index = 63;
                while (index >= 0)
                {
                    if ((m_Filter & (1L << index)) != 0)
                    {
                        return index;
                    }

                    index--;
                }

                throw new InvalidOperationException("No condition in this query");
            }
        }

        /// <summary>
        /// Take last <see cref="Condition"/> in this query
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public Condition Last
        {
            get
            {
                int index = 63;
                while (index >= 0)
                {
                    if ((m_Filter & (1L << index)) != 0)
                    {
                        return (Condition)(index + m_Offset);
                    }

                    index--;
                }

                throw new InvalidOperationException("No condition in this query");
            }
        }

        /// <summary>
        /// Base constructor.
        /// End user should not use this constructor due to implemented by operator
        /// </summary>
        /// <param name="c"></param>
        /// <param name="filter"></param>
        private ConditionQuery(short c, long filter)
        {
            m_Offset = c;
            m_Filter = filter;
        }

        /// <summary>
        /// Returns given condition is in this query.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        [Pure]
        public bool Has(Condition c)
        {
            short s = (short)c;
            int  o = s - m_Offset;

            if (s < m_Offset || 64 <= o) return false;

            long f = 1L << o;
            return (m_Filter & f) == f;
        }

        /// <summary>
        /// Returns given condition index that relative from this query
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        [Pure]
        public int IndexOf(Condition c)
        {
            short s = (short)c;
            int  o = s - m_Offset;
            long f = 1L << o;

            if (s < m_Offset || 64 <= o || (m_Filter & f) != f) return -1;

            long t     = 1L << o;
            int  index = 0;
            while (index < 64)
            {
                if ((t & (1L << index)) != 0)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Iterate all conditions in this query
        /// </summary>
        /// <returns></returns>
        [Pure]
        public IEnumerator<Condition> GetEnumerator()
        {
            int index = 0;
            while (index < 64)
            {
                if ((m_Filter & (1L << index)) != 0)
                {
                    yield return (Condition)(index + m_Offset);
                }
                index++;
            }
        }

        public bool Equals(ConditionQuery y)
        {
            ConditionQuery q = this & y;
            return q.m_Offset == m_Offset   && q.m_Filter == m_Filter &&
                   q.m_Offset == y.m_Offset && q.m_Filter == y.m_Filter;
        }

        public override bool Equals(object obj)
        {
            return obj is ConditionQuery other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Offset.GetHashCode() * 397) ^ m_Filter.GetHashCode();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator ConditionQuery(Condition c) => new ConditionQuery((short)c, 1);

        /// <summary>
        /// This is (x & y) == y short-handed operator
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool operator ==(ConditionQuery x, ConditionQuery y)
        {
            ConditionQuery q = x & y;
            return q.m_Offset == x.m_Offset && q.m_Filter == x.m_Filter &&
                   q.m_Offset == y.m_Offset && q.m_Filter == y.m_Filter;
        }
        /// <summary>
        /// This is (x & y) != y short-handed operator
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool operator !=(ConditionQuery x, ConditionQuery y)
        {
            return !(x == y);
        }

        public static ConditionQuery operator |(ConditionQuery x, ConditionQuery y)
        {
            if (x.m_Filter == 0) return y;
            if (y.m_Filter == 0) return x;

            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            if (64 <= math.abs(o - x.m_Offset) ||
                64 <= math.abs(o - y.m_Offset))
                throw new InvalidOperationException($"exceed query");
            if (0 < y.m_Filter && x.m_Offset + 63 < (int)y.Last)
                throw new InvalidOperationException($"exceed query");

            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << math.abs(o - y.m_Offset);

            return new ConditionQuery(o, xf | yf);
        }
        public static ConditionQuery operator &(ConditionQuery x, ConditionQuery y)
        {
            if (64 <= math.abs(x.m_Offset - y.m_Offset)) return default;

            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            if (64 <= math.abs(o - x.m_Offset) ||
                64 <= math.abs(o - y.m_Offset))
                throw new InvalidOperationException($"exceed query");

            int yo = math.abs(o - y.m_Offset);
            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << yo;
            yo -= 64;
            while (0 < yo)
            {
                yf &= ~(1L << yo--);
            }

            return new ConditionQuery(o, xf & yf);
        }
        public static ConditionQuery operator ^(ConditionQuery x, ConditionQuery y)
        {
            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << math.abs(o - y.m_Offset);

            return new ConditionQuery(o, xf ^ yf);
        }

        /// <summary>
        /// Take out the condition from x
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static ConditionQuery operator -(ConditionQuery x, Condition y)
        {
            ConditionQuery yy = y;
            if ((x & yy) == yy) return x ^ yy;
            return x;
        }
    }
}