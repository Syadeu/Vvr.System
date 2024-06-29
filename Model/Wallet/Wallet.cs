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

namespace Vvr.Model.Wallet
{
    [JsonConverter(typeof(UnresolvedWalletJsonConverter))]
    [SheetValueConverter(typeof(UnresolvedWalletConverter))]
    public readonly struct Wallet : IReadOnlyWallet
    {
        public static Wallet Create(ShortFlag64<WalletType> t)
        {
            return new Wallet(t, new float[t.Count]);
        }

        private readonly float[] m_Values;

        public float this[WalletType t]
        {
            get => GetValue(t);
            set => SetValue(t, value);
        }

        public ShortFlag64<WalletType> Types  { get; }
        public IList<float>            Values => m_Values;

        IReadOnlyList<float> IReadOnlyWallet.Values => m_Values;

        internal Wallet(ShortFlag64<WalletType> query, float[] values)
        {
            Types    = query;
            m_Values = values;
        }

        public float GetValue(WalletType t)
        {
            int index = Types.IndexOf(t);
            if (index < 0) return 0;

            return Values[index];
        }
        public void SetValue(WalletType t, float v)
        {
            int index = Types.IndexOf(t);
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
                var q = (WalletType)i;
                if (!result.Types.Contains((WalletType)i)) continue;

                if (x.Types.Contains(q)) result.m_Values[c] = x.m_Values[xx++];

                // var e = 1L << i;
                // if (((long)result.Types & e) == 0) continue;
                //
                // if (((long)x.Types & e) != 0) result.m_Values[c] = x.m_Values[xx++];
                c++;
            }
            return result;
        }
        public static Wallet operator +(Wallet x, Wallet y)
        {
            if (y.Values == null) return x;

            var newTypes = (x.Types | y.Types);
            var result   = Create(newTypes);

            int maxIndex = result.Values.Count;
            for (int i = 0, c = 0, xx = 0, yy = 0; i < 64 && c < maxIndex; i++)
            {
                var q = (WalletType)i;
                if (!newTypes.Contains((WalletType)i)) continue;

                if (x.Types.Contains(q)) result.m_Values[c] =  x.m_Values[xx++];
                if (y.Types.Contains(q)) result.m_Values[c] += y.Values[yy++];

                // var e = 1L << i;
                // if (((long)newTypes & e) == 0) continue;
                //
                // if (((long)x.Types & e) != 0) result.m_Values[c] =  x.m_Values[xx++];
                // if (((long)y.Types & e) != 0) result.m_Values[c] += y.Values[yy++];
                c++;
            }

            return result;
        }
        public static Wallet operator -(Wallet x, Wallet y)
        {
            if (y.Values == null) return x;

            var newTypes = (x.Types | y.Types);
            var result   = Create(x.Types | y.Types);

            int maxIndex = result.Values.Count;
            for (int i = 0, c = 0, xx = 0, yy = 0; i < 64 && c < maxIndex; i++)
            {
                var q = (WalletType)i;
                if (!newTypes.Contains((WalletType)i)) continue;

                if (x.Types.Contains(q)) result.m_Values[c] =  x.m_Values[xx++];
                if (y.Types.Contains(q)) result.m_Values[c] -= y.Values[yy++];

                // var e = 1L << i;
                // if (((long)newTypes & e) == 0) continue;
                //
                // if (((long)x.Types & e) != 0) result.m_Values[c] =  x.m_Values[xx++];
                // if (((long)y.Types & e) != 0) result.m_Values[c] -= y.Values[yy++];
                c++;
            }

            return result;
        }

        public IEnumerator<KeyValuePair<WalletType, float>> GetEnumerator()
        {
            // long target = (long)Types;
            for (int i = 0, c = 0; i < 64; i++)
            {
                var q = (WalletType)i;
                if (!Types.Contains(q)) continue;

                yield return new(q, m_Values[c++]);

                // long e = 1L << i;
                // if ((target & e) != e) continue;

                // yield return new((WalletType)e, m_Values[c++]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}