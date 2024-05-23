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
// File created : 2024, 05, 23 01:05

#endregion

using System.Collections.Generic;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [UsedImplicitly, Preserve]
    public sealed class ResearchSheet : Sheet<ResearchSheet.Row>
    {
        public struct Definition
        {
            [UsedImplicitly] public int                 Group   { get; private set; }
            [UsedImplicitly] public int                 Order   { get; private set; }
            [UsedImplicitly] public int                 MaxLevel   { get; private set; }
            [UsedImplicitly] public StatSheet.Reference TargetStat { get; private set; }
        }
        public struct Methods
        {
            [UsedImplicitly] public CustomMethodSheet.Reference ResearchTime { get; private set; }
            [UsedImplicitly] public CustomMethodSheet.Reference Consumable   { get; private set; }
            [UsedImplicitly] public CustomMethodSheet.Reference StatModifier { get; private set; }
        }
        
        public sealed class Row : SheetRow
        {
            [UsedImplicitly] public Definition      Definition { get; private set; }
            [UsedImplicitly] public Methods         Methods    { get; private set; }
            [UsedImplicitly] public List<Reference> Connection { get; private set; }
        }

        public ResearchSheet()
        {
            Name = nameof(GameDataSheets.ResearchTable);
        }
    }
}