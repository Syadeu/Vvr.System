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
// File created : 2024, 05, 05 15:05

#endregion

using System.Collections.Generic;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.System.Model
{
    [Preserve]
    public sealed class ItemSheet : Sheet<ItemSheet.Row>
    {
        public enum Category : short
        {
            Default = 0,

            Weapon,
            Ring,
            Neckless
        }

        public struct Definition
        {
            [UsedImplicitly] public Category  Category  { get; private set; }
            [UsedImplicitly] public int       ItemType  { get; private set; }
            [UsedImplicitly] public int       Grade     { get; private set; }
            [UsedImplicitly] public float     LevelCost { get; private set; }
            [UsedImplicitly] public Reference NextItem  { get; private set; }
        }

        public sealed class Row : SheetRow
        {
            [UsedImplicitly] public Definition Definition { get; private set; }

            [SheetValueConverter(typeof(UnresolvedWalletConverter))]
            [UsedImplicitly] public IReadOnlyWallet Fragment { get; private set; }
            [SheetValueConverter(typeof(UnresolvedWalletConverter))]
            [UsedImplicitly] public IReadOnlyWallet Transcend { get; private set; }

            [SheetValueConverter(typeof(UnresolvedStatValuesConverter))]
            [UsedImplicitly] public IReadOnlyStatValues Stats { get; private set; }
            [SheetValueConverter(typeof(UnresolvedStatValuesConverter))]
            [UsedImplicitly] public IReadOnlyStatValues UpgradeStats { get; private set; }

            [UsedImplicitly] public List<AbnormalSheet.Reference> Abnormal { get; private set; }
        }

        public ItemSheet()
        {
            Name = "Items";
        }
    }

    public static class ItemSheetExtensions
    {

    }
}