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
// File created : 2024, 05, 05 12:05

#endregion

using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using Vvr.Model.Stat;

namespace Vvr.Model
{
    [Preserve]
    public sealed class StatSheet : Sheet<StatSheet.Row>
    {
        public sealed class Row : SheetRow, IStatData
        {
            [UsedImplicitly] public string Icon { get; private set; }
            // [UsedImplicitly] public Reference Child   { get; private set; }

            object IStatData.IconAssetKey => Icon.IsNullOrEmpty() ? null : Icon;
        }

        public StatSheet()
        {
            Name = nameof(GameDataSheets.StatTable);
        }
    }
    public static class StatSheetExtensions
    {
        /// <summary>
        /// Converts the given <see cref="StatSheet.Row"/> object to a <see cref="StatType"/>.
        /// </summary>
        /// <param name="t">The <see cref="StatSheet.Row"/> object to convert.</param>
        /// <returns>The <see cref="StatType"/> converted from the <see cref="StatSheet.Row"/> object.</returns>
        public static StatType ToStat(this IStatData t)
        {
            Assert.IsTrue(0 <= t.Index);
            Assert.IsTrue(t.Index < 64);

            int i = t.Index;
            return (StatType)(1L << i);
        }
    }

    [PublicAPI]
    public interface IStatData : IRawData
    {
        object IconAssetKey { get; }
    }
}