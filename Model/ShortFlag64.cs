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
// File created : 2024, 06, 29 00:06

#endregion

using System;
using System.Globalization;
using JetBrains.Annotations;
using Unity.Mathematics;

namespace Vvr.Model
{
    [PublicAPI]
    public readonly struct ShortFlag64<TEnum> : IEquatable<ShortFlag64<TEnum>> where TEnum : struct, IConvertible
    {
        private readonly short m_Offset;
        private readonly long  m_Filter;

        [PublicAPI]
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

        [PublicAPI]
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

        [PublicAPI]
        public Condition First
        {
            get
            {
                int index = 0;
                while (index < 64)
                {
                    if ((m_Filter & (1L << index)) != 0)
                    {
                        return (Condition)(index + m_Offset);
                    }

                    index++;
                }

                throw new InvalidOperationException("No condition in this query");
            }
        }

        [PublicAPI]
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

        [PublicAPI] public bool IsEmpty => m_Filter == 0 && m_Offset == 0;

        private ShortFlag64(short c, long filter)
        {
            m_Offset = c;
            m_Filter = filter;
        }

        [Pure, MustUseReturnValue]
        public bool Contains(TEnum c)
        {
            short s = c.ToInt16(NumberFormatInfo.InvariantInfo);
            int   o = s - m_Offset;

            if (s < m_Offset || 64 <= o) return false;

            long f = 1L << o;
            return (m_Filter & f) == f;
        }

        [Pure, MustUseReturnValue]
        public int IndexOf(TEnum c)
        {
            short s = c.ToInt16(NumberFormatInfo.InvariantInfo);
            int   o = s - m_Offset;
            long  f = 1L << o;

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

        public bool Equals(ShortFlag64<TEnum> other)
        {
            return m_Offset == other.m_Offset && m_Filter == other.m_Filter;
        }
        public override bool Equals(object obj)
        {
            return obj is ShortFlag64<TEnum> other && Equals(other);
        }
        public override int GetHashCode() => unchecked((int)((m_Offset * 397) ^ m_Filter));

        public static implicit operator ShortFlag64<TEnum>(TEnum c) =>
            new(c.ToInt16(NumberFormatInfo.InvariantInfo), 1);

        public static bool operator ==(ShortFlag64<TEnum> x, ShortFlag64<TEnum> y)
        {
            ShortFlag64<TEnum> q = x & y;
            return q.m_Offset == x.m_Offset && q.m_Filter == x.m_Filter &&
                   q.m_Offset == y.m_Offset && q.m_Filter == y.m_Filter;
        }

        public static bool operator !=(ShortFlag64<TEnum> x, ShortFlag64<TEnum> y)
        {
            return !(x == y);
        }

        public static ShortFlag64<TEnum> operator |(ShortFlag64<TEnum> x, ShortFlag64<TEnum> y)
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

            return new ShortFlag64<TEnum>(o, xf | yf);
        }

        public static ShortFlag64<TEnum> operator &(ShortFlag64<TEnum> x, ShortFlag64<TEnum> y)
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

            return new ShortFlag64<TEnum>(o, xf & yf);
        }

        public static ShortFlag64<TEnum> operator ^(ShortFlag64<TEnum> x, ShortFlag64<TEnum> y)
        {
            short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
            long xf = x.m_Filter << math.abs(o - x.m_Offset),
                yf  = y.m_Filter << math.abs(o - y.m_Offset);

            return new ShortFlag64<TEnum>(o, xf ^ yf);
        }

        public static ShortFlag64<TEnum> operator -(ShortFlag64<TEnum> x, TEnum y)
        {
            ShortFlag64<TEnum> yy = y;
            if ((x & yy) == yy) return x ^ yy;
            return x;
        }
    }
}