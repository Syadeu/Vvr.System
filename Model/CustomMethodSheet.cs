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
// File created : 2024, 05, 15 13:05

#endregion

using System.Collections.Generic;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [Preserve]
    public sealed class CustomMethodSheet : Sheet<CustomMethodSheet.Row>
    {
        public class Row : SheetRowArray<Variable>
        {
            [UsedImplicitly] public List<string> Calculations { get; private set; }
        }
        public class Variable : SheetRowElem
        {
            [UsedImplicitly] public string           Name  { get; private set; }
            [UsedImplicitly] public DynamicReference Value { get; private set; }
        }

        public CustomMethodSheet()
        {
            Name = nameof(GameDataSheets.CustomMethodTable);
        }
    }
}