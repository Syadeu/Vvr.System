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
// File created : 2024, 05, 11 12:05

#endregion

using System.Collections;
using System.Collections.Generic;
using Cathei.BakingSheet;
using Newtonsoft.Json;
using UnityEngine.Assertions;
using NotImplementedException = System.NotImplementedException;

namespace Vvr.System.Model
{
    public delegate float WalletGetterDelegate(in Wallet wallet);
    public delegate void WalletSetterDelegate(in Wallet wallet, float value);

    [JsonConverter(typeof(UnresolvedWalletJsonConverter))]
    [SheetValueConverter(typeof(UnresolvedWalletConverter))]
    public readonly struct Wallet : IReadOnlyWallet
    {
        private static readonly Dictionary<WalletType, WalletGetterDelegate>
            s_CachedGetter = new();

        private static readonly Dictionary<WalletType, WalletSetterDelegate>
            s_CachedSetter = new();

        public static WalletGetterDelegate GetGetMethod(WalletType t)
        {
            if (!s_CachedGetter.TryGetValue(t, out var d))
            {
                d                 = (in Wallet x) => x[t];
                s_CachedGetter[t] = d;
            }

            return d;
        }

        public static WalletSetterDelegate GetSetMethod(WalletType t)
        {
            if (!s_CachedSetter.TryGetValue(t, out var d))
            {
                d                 = (in Wallet x, float value) => x.SetValue(t, value);
                s_CachedSetter[t] = d;
            }

            return d;
        }

        public static Wallet Create(WalletType t = (WalletType)~0)
        {
            return new Wallet(t, new float[t.Count()]);
        }

        private readonly float[] m_Values;

        public float this[WalletType t]
        {
            get => GetValue(t);
            set => SetValue(t, value);
        }

        public WalletType   Types  { get; }
        public IList<float> Values => m_Values;

        IReadOnlyList<float> IReadOnlyWallet.Values => m_Values;

        internal Wallet(WalletType query, float[] values)
        {
            Types    = query;
            m_Values = values;
        }

        public int IndexOf(WalletType t)
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

        public float GetValue(WalletType t)
        {
            int index = IndexOf(t);
            if (index < 0) return 0;

            return Values[index];
        }
        public void SetValue(WalletType t, float v)
        {
            int index = IndexOf(t);
            Assert.IsFalse(index < 0);

            Values[index] = v;
        }

        public static Wallet operator |(Wallet x, WalletType t)
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
        public static Wallet operator +(Wallet x, Wallet y)
        {
            if (y.Values == null) return x;

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
        public static Wallet operator -(Wallet x, Wallet y)
        {
            if (y.Values == null) return x;

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

        public IEnumerator<KeyValuePair<WalletType, float>> GetEnumerator()
        {
            long target = (long)Types;
            for (int i = 0, c = 0; i < 64; i++)
            {
                long e = 1L << i;
                if ((target & e) != e) continue;

                yield return new((WalletType)e, m_Values[c++]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}