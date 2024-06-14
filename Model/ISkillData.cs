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
// File created : 2024, 06, 14 19:06

#endregion

using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model.Stat;

namespace Vvr.Model
{
    /// <summary>
    /// Represents a skill data.
    /// </summary>
    [PublicAPI]
    public interface ISkillData : ISkillID, IRawData
    {
        float Cooltime { get; }
        float Delay    { get; }

        int                 TargetCount { get; }
        SkillSheet.Target   Target      { get; }
        SkillSheet.Position Position    { get; }

        SkillSheet.Method Method     { get; }
        float             Multiplier { get; }
        StatType          TargetStat { get; }

        [CanBeNull] object IconAssetKey          { get; }
        [CanBeNull] object SelfEffectAssetKey    { get; }
        [CanBeNull] object CastingEffectAssetKey { get; }
        [CanBeNull] object TargetEffectAssetKey  { get; }

        [CanBeNull] IReadOnlyList<AbnormalSheet.Reference> Abnormal { get; }
    }
}