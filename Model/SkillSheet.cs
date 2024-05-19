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
// File created : 2024, 05, 05 01:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet;
using Cathei.BakingSheet.Unity;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [Preserve]
    public sealed class SkillSheet : Sheet<SkillSheet.Row>
    {
        [Flags]
        public enum Target : int
        {
            ERROR = 0,

            Self  = 0b001,
            Ally  = 0b010,
            Enemy = 0b100,
            Both  = 0b011,

            All   = 0b111
        }

        [Flags]
        public enum Position : int
        {
            All  = 0,
            Forward  = 0b01,
            Backward = 0b10,
            Random   = 0b11
        }

        public enum Method
        {
            Default,

            Damage,
        }

        public struct Definition
        {
            [UsedImplicitly] public Target Target   { get; private set; }
            [UsedImplicitly] public Position    Position { get; private set; }

            [UsedImplicitly] public int         TargetCount { get; private set; }
            [UsedImplicitly] public float       Cooltime    { get; private set; }
            [UsedImplicitly] public float       Delay    { get; private set; }
        }

        public struct Execution
        {
            [UsedImplicitly] public Method              Method     { get; private set; }
            [UsedImplicitly] public StatSheet.Reference TargetStat { get; private set; }
            [UsedImplicitly] public float               Multiplier { get; private set; }
        }

        public struct Presentation
        {
            [UsedImplicitly] public AddressablePath SelfEffect   { get; private set; }
            [UsedImplicitly] public AddressablePath TargetEffect { get; private set; }
        }

        public sealed class Row : SheetRow, ISkillData
        {
            [UsedImplicitly] public Reference   NextLevel  { get; private set; }

            [UsedImplicitly] public Definition   Definition   { get; private set; }
            [UsedImplicitly] public Execution    Execution    { get; private set; }
            [UsedImplicitly] public Presentation Presentation { get; private set; }

            [UsedImplicitly] public List<AbnormalSheet.Reference> Abnormal { get; private set; }

            float ISkillData.Cooltime => Definition.Cooltime;
        }

        public SkillSheet()
        {
            Name = "Skills";
        }
    }

    public interface ISkillID
    {
        string Id { get; }
    }

    public interface ISkillData : ISkillID
    {
        float Cooltime { get; }
    }

    public static class SkillSheetExtensions
    {
        [MustUseReturnValue]
        public static float CalculateCooltime(this SkillSheet.Row t, float speed)
        {
            float cooltime = t.Definition.Cooltime;

            if (speed < 100)
            {
                speed *= 0.03f;
            }
            else
            {
                speed = math.log2(speed + 1) * 0.05f;
            }

            cooltime -= cooltime * speed;
            return cooltime;
        }
    }
}