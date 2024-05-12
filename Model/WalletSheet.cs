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

using Cathei.BakingSheet;
using UnityEngine.Scripting;

namespace Vvr.System.Model
{
    [Preserve]
    public sealed class WalletSheet : Sheet<WalletSheet.Row>
    {
        public sealed class Row : SheetRow
        {
        }

        public WalletSheet()
        {
            Name = nameof(GameDataSheets.WalletTable);
        }
    }

    public static class WalletSheetExtensions
    {
        public static WalletType ToWallet(this WalletSheet.Row t)
        {
            int i = t.Index;
            return (WalletType)(1L << i);
        }

        // public static WalletSheet.Row Resolve(this WalletType t) => WalletProvider.Static[t];
    }
}