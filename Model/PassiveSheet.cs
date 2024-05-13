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

using System;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [Preserve]
    public sealed class PassiveSheet : Sheet<PassiveSheet.Row>
    {
        [Flags]
        public enum Target : int
        {
            ERROR = 0,

            Self  = SkillSheet.Target.Self,
            Ally  = SkillSheet.Target.Ally,
            Enemy = SkillSheet.Target.Enemy,
            Both  = SkillSheet.Target.Both,
            All   = SkillSheet.Target.All,
        }

        [Flags]
        public enum Position : int
        {
            All  = SkillSheet.Position.All,
            Forward  = SkillSheet.Position.Forward,
            Backward = SkillSheet.Position.Backward,
            Random   = SkillSheet.Position.Random
        }
        public enum AffectType
        {
            Map = 0,

            Stage,
            Floor,
            Region,
        }
        public enum ConclusionType
        {
            Skill,
            Abnormal
        }

        public struct Definition
        {
            [UsedImplicitly] public int        Type       { get; private set; }
            [UsedImplicitly] public int        Level      { get; private set; }
            [UsedImplicitly] public AffectType AffectType { get; private set; }
        }
        public struct Activation
        {
            [UsedImplicitly] public Condition Condition { get; private set; }
            [UsedImplicitly] public string    Value     { get; private set; }
        }
        public struct Execution
        {
            [UsedImplicitly] public Condition Condition   { get; private set; }
            [UsedImplicitly] public string    Value       { get; private set; }
            [UsedImplicitly] public float     Probability { get; private set; }
        }
        public struct Conclusion
        {
            [UsedImplicitly] public Target           Target   { get; private set; }
            [UsedImplicitly] public Position         Position { get; private set; }
            [UsedImplicitly] public ConclusionType   Type     { get; private set; }
            [UsedImplicitly] public DynamicReference Value    { get; private set; }
        }

        public sealed class Row : SheetRow
        {
            [UsedImplicitly] public string     Guid       { get; private set; }
            [UsedImplicitly] public Definition Definition { get; private set; }
            [UsedImplicitly] public Activation Activation { get; private set; }
            [UsedImplicitly] public Execution  Execution  { get; private set; }
            [UsedImplicitly] public Conclusion Conclusion { get; private set; }

            public override void PostLoad(SheetConvertingContext context)
            {
                base.PostLoad(context);

                object v;
                if (Conclusion.Type == ConclusionType.Abnormal)
                {
                    v = context.Container.Find<AbnormalSheet>(nameof(GameDataSheets.AbnormalSheet))[Conclusion.Value.Id];
                }
                else
                {
                    v = context.Container.Find<SkillSheet>("Skills")[Conclusion.Value.Id];
                }

                var boxed = Conclusion;
                boxed.Value.SetReference(v);
                Conclusion = boxed;
            }
        }

        public PassiveSheet()
        {
            Name = "Passive";
        }
    }
}