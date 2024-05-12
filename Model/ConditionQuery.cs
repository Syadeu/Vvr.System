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
using Unity.Mathematics;

namespace Vvr.System.Model
{
    public readonly struct ConditionQuery : IEquatable<ConditionQuery>
    {
        public static ConditionQuery All => new ConditionQuery(0, ~0L);

        private readonly short m_Offset;
        private readonly long  m_Filter;

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

        private ConditionQuery(short c, long filter)
        {
            m_Offset = c;
            m_Filter = filter;
        }

        public bool Has(Condition c)
        {
            short s = (short)c;
            if (s < m_Offset) return false;

            int  o = s - m_Offset;
            long f = 1L << o;
            return (m_Filter & f) == f;
        }

        public bool Equals(ConditionQuery other)
        {
            return m_Offset == other.m_Offset && m_Filter == other.m_Filter;
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

        public static implicit operator ConditionQuery(Condition c) => new ConditionQuery((short)c, 1);

        public static bool operator ==(ConditionQuery x, ConditionQuery y)
        {
            return x.m_Offset == y.m_Offset && x.m_Filter == y.m_Filter;
        }
        public static bool operator !=(ConditionQuery x, ConditionQuery y)
        {
            return !(x == y);
        }

        public static ConditionQuery operator |(ConditionQuery x, ConditionQuery y)
        {
            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << math.abs(o - y.m_Offset);

            return new ConditionQuery(o, xf | yf);
        }

        public static ConditionQuery operator &(ConditionQuery x, ConditionQuery y)
        {
            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << math.abs(o - y.m_Offset);

            return new ConditionQuery(o, xf & yf);
        }
        public static ConditionQuery operator ^(ConditionQuery x, ConditionQuery y)
        {
            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << math.abs(o - y.m_Offset);

            return new ConditionQuery(o, xf ^ yf);
        }

        public static ConditionQuery operator -(ConditionQuery x, Condition y)
        {
            ConditionQuery yy = y;
            if ((x & yy) == yy) return x ^ yy;
            return x;
        }
    }
}