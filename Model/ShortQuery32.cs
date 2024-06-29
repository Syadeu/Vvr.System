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
// File created : 2024, 06, 28 16:06

#endregion

using System;
using System.Globalization;

namespace Vvr.Model
{
    public readonly struct ShortQuery32<TEnum> : IEquatable<ShortQuery32<TEnum>>
        where TEnum : struct, IConvertible
    {
        private readonly int m_Flag;

        private ShortQuery32(TEnum @enum)
        {
            short v  = @enum.ToInt16(NumberFormatInfo.InvariantInfo);
            int   lv = v ^ 267;

            m_Flag = lv;
        }
        private ShortQuery32(int f)
        {
            m_Flag = f;
        }

        public static implicit operator ShortQuery32<TEnum>(TEnum x) => new(x);

        public static ShortQuery32<TEnum> operator ^(ShortQuery32<TEnum> x, ShortQuery32<TEnum> y)
        {
            int f = x.m_Flag;
            f ^= y.m_Flag;

            return new ShortQuery32<TEnum>(f);
        }

        public static bool operator ==(ShortQuery32<TEnum> x, ShortQuery32<TEnum> y)
        {
            return x.m_Flag == y.m_Flag;
        }

        public static bool operator !=(ShortQuery32<TEnum> x, ShortQuery32<TEnum> y)
        {
            return !(x == y);
        }

        public bool Equals(ShortQuery32<TEnum> other)
        {
            return m_Flag == other.m_Flag;
        }

        public override bool Equals(object obj)
        {
            return obj is ShortQuery32<TEnum> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Flag;
        }
    }
}