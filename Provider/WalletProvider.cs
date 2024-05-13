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

using System.Collections.Generic;
using Vvr.Model;
using Vvr.Model.Wallet;

namespace Vvr.MPC.Provider
{
    public sealed class WalletProvider
    {
        public static WalletProvider Static { get; private set; }

        public static WalletProvider GetOrCreate(WalletSheet sheet)
        {
            Static ??= new WalletProvider(sheet);
            return Static;
        }

        private readonly Dictionary<string, WalletType>          m_Map     = new();
        private readonly Dictionary<WalletType, WalletSheet.Row> m_DataMap = new();

        public WalletSheet.Row this[WalletType t] => m_DataMap[t];

        private WalletProvider(WalletSheet t)
        {
            foreach (var row in t)
            {
                int        i    = row.Index;
                WalletType type = (WalletType)(1L << i);
                m_Map[row.Id]   = type;
                m_DataMap[type] = row;
            }
        }
    }
}