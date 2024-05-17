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
// File created : 2024, 05, 16 22:05

#endregion

using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Session.Actor
{
    internal sealed class StageActor : IStageActor
    {
        public readonly IActor     owner;
        public readonly IActorData data;

        public bool TagOutRequested { get; set; }

        public StageActor(IActor o, IActorData d)
        {
            owner = o;
            data  = d;
        }

        IActor IStageActor.        Owner       => owner;
        string IRawData.  Id   => owner.Id;
        string IActorData.Guid => data.Guid;

        ActorSheet.ActorType IActorData.Type       => data.Type;
        int IActorData.                 Population => data.Population;
        IReadOnlyStatValues IActorData. Stats      => data.Stats;

        IReadOnlyList<PassiveSheet.Row> IActorData.Passive => data.Passive;
        IReadOnlyList<SkillSheet.Row> IActorData.  Skills  => data.Skills;

        Dictionary<AssetType, AddressablePath> IActorData.Assets => data.Assets;
    }
}