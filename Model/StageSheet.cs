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
using System.Linq;
using Cathei.BakingSheet;
using Cathei.BakingSheet.Unity;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [Preserve]
    public sealed class StageSheet : Sheet<StageSheet.Row>
    {
        [UsedImplicitly]
        public struct Definition
        {
            [UsedImplicitly] public int Region { get; private set; }
            [UsedImplicitly] public int Floor { get; private set; }
        }

        public sealed class Row : SheetRow, IStageData
        {
            [UsedImplicitly] public string Name { get; private set; }
            [UsedImplicitly] public int Population { get; private set; }

            [UsedImplicitly] public Definition                 Definition  { get; private set; }
            [UsedImplicitly] public List<ActorSheet.Reference> Actors         { get; private set; }

            [UsedImplicitly] public Dictionary<AssetType, AddressablePath> Assets { get; private set; }

            private IActorData[] m_Actors;
            private bool         m_IsLastStage;
            private bool         m_IsLastOfRegion;

            int IStageData. Region         => Definition.Region;
            int IStageData. Floor          => Definition.Floor;
            bool IStageData.IsLastStage    => m_IsLastStage;
            bool IStageData.IsLastOfRegion => m_IsLastOfRegion;

            IReadOnlyList<IActorData> IStageData.                      Actors    => m_Actors;
            IReadOnlyDictionary<AssetType, AddressablePath> IStageData.Assets    => Assets;

            public override void PostLoad(SheetConvertingContext context)
            {
                base.PostLoad(context);

                m_Actors =
                    Actors != null && Actors.Count > 0 ?
                    Actors.Select(x => (IActorData)x.Ref).ToArray()
                        :
                    Array.Empty<IActorData>();
            }

            internal void Build(StageSheet stageSheet)
            {
                m_IsLastOfRegion = Index + 1 >= stageSheet.Count;

                if (!m_IsLastOfRegion)
                {
                    var nextRow = stageSheet[Index + 1];
                    m_IsLastStage = nextRow.Definition.Region != Definition.Region ||
                                    nextRow.Definition.Floor  != Definition.Floor;
                }
                else m_IsLastStage = true;
            }
        }

        public StageSheet()
        {
            Name = "Stages";
        }

        public override void PostLoad(SheetConvertingContext context)
        {
            base.PostLoad(context);

            var stageSheet = context.Container.Find<StageSheet>(nameof(GameDataSheets.Stages));
            foreach (var row in stageSheet)
            {
                row.Build(stageSheet);
            }
        }
    }
}