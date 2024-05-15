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
// File created : 2024, 05, 02 10:05

#endregion

using Cathei.BakingSheet;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Vvr.Model
{
    public sealed class GameDataSheets : SheetContainerBase
    {
        public GameDataSheets(ILogger logger) : base(logger)
        {
        }

        [UsedImplicitly] public GameConfigSheet   GameConfigTable   { get; private set; }
        [UsedImplicitly] public CustomMethodSheet CustomMethodTable { get; private set; }
        [UsedImplicitly] public WalletSheet       WalletTable       { get; private set; }
        [UsedImplicitly] public StatSheet         StatTable         { get; private set; }

        [UsedImplicitly] public ActorSheet Actors { get; private set; }
        [UsedImplicitly] public StageSheet Stages { get; private set; }
        [UsedImplicitly] public LevelSheet LevelTable { get; private set; }

        [UsedImplicitly] public SkillSheet Skills    { get; private set; }

        [UsedImplicitly] public AbnormalSheet AbnormalSheet { get; private set; }
        [UsedImplicitly] public PassiveSheet  PassiveSheet  { get; private set; }

        [UsedImplicitly] public ItemSheet Items { get; private set; }
    }
}