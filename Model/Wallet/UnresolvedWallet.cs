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

namespace Vvr.Model.Wallet
{
    [JsonConverter(typeof(UnresolvedWalletJsonConverter))]
    class UnresolvedWallet : UnresolvedValues<Wallet>, IReadOnlyWallet
    {
        protected override Wallet Resolve(ISheet s)
        {
            WalletSheet sheet = s as WalletSheet;
            Assert.IsNotNull(sheet, "sheet != null");

            float[] v = new float[values.Length];

            int                     i     = 0;
            ShortFlag64<WalletType> query = default;
            // long query = 0;
            foreach (var item in ids)
            {
                int index = sheet[item].Index;

                WalletType e = (WalletType)index;
                // long e = 1L << index;
                query |= e;

                v[i] = values[i];
                i++;
            }

            return new Wallet(query, v);
        }

        public IEnumerator<KeyValuePair<WalletType, float>> GetEnumerator() => Value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public float this[WalletType t] => Value[t];

        public ShortFlag64<WalletType> Types  => Value.Types;
        public IReadOnlyList<float>    Values => (IReadOnlyList<float>)Value.Values;
    }
}