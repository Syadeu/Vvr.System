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
// File created : 2024, 05, 17 00:05

#endregion

using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model.Stat;

namespace Vvr.Model
{
    /// <summary>
    /// Represents the interface for actor data.
    /// </summary>
    [PublicAPI]
    public interface IActorData : IRawData
    {
        ActorSheet.ActorType Type       { get; }
        int                  Grade      { get; }
        int                  Population { get; }

        IRawStatValues                  Stats   { get; }
        IReadOnlyList<PassiveSheet.Row> Passive { get; }
        IReadOnlyList<ISkillData>       Skills  { get; }
        Dictionary<AssetType, string>   Assets  { get; }
    }
}