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
// File created : 2024, 05, 02 09:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet;
using Cathei.BakingSheet.Unity;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Model.Stat;

namespace Vvr.Model
{
    [Preserve]
    public sealed class ActorSheet : Sheet<ActorSheet.Row>
    {
        [Flags]
        public enum ActorType : short
        {
            Default   = 0b01,
            Offensive = 0,
            Defensive = 0b10,

            All = 0b11
        }

        public sealed class Row : SheetRow, IActorData
        {
            [UsedImplicitly] public string    Guid       { get; private set; }
            [UsedImplicitly] public ActorType Type       { get; private set; }
            [UsedImplicitly] public int       Population { get; private set; }

            [SheetValueConverter(typeof(UnresolvedStatValuesConverter))]
            [UsedImplicitly]
            public IReadOnlyStatValues Stats { get; private set; }

            [UsedImplicitly] public List<PassiveSheet.Reference>    Passive { get; private set; }
            [UsedImplicitly] public List<SkillSheet.Reference> Skills  { get; private set; }

            [UsedImplicitly] public Dictionary<AssetType, AddressablePath> Assets { get; private set; }

            IReadOnlyList<PassiveSheet.Reference> IActorData.Passive => Passive;
            IReadOnlyList<SkillSheet.Reference> IActorData.Skills => Skills;
        }

        public ActorSheet()
        {
            Name = "Actors";
        }

        public override void PostLoad(SheetConvertingContext context)
        {
            base.PostLoad(context);

            var stat = context.Container.Find<StatSheet>(nameof(GameDataSheets.StatTable));
            foreach (Row row in Items)
            {
                if (row.Stats is UnresolvedStatValues unresolvedStatValues)
                {
                    unresolvedStatValues.Build(stat);
                }
            }
        }
    }
}