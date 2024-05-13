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
// File created : 2024, 05, 07 19:05

#endregion

using System;
using UnityEngine.Assertions;
using Vvr.Model;

namespace Vvr.Controller.Skill
{
    public interface ITargetDefinition
    {
        SkillSheet.Target Target   { get; }
        SkillSheet.Position    Position { get; }
    }

    public readonly struct TargetDefinition : ITargetDefinition
    {
        private readonly SkillSheet.Target    m_Target;
        private readonly SkillSheet.Position  m_Position;

        SkillSheet.Target ITargetDefinition.  Target   => m_Target;
        SkillSheet.Position ITargetDefinition.Position => m_Position;

        public TargetDefinition(SkillSheet.Target t, SkillSheet.Position p)
        {
            m_Target   = t;
            m_Position = p;
        }
        public TargetDefinition(PassiveSheet.Target t, PassiveSheet.Position p)
        {
            SkillSheet.Target target = (SkillSheet.Target)t;
            Assert.IsTrue(Enum.IsDefined(VvrTypeHelper.TypeOf<SkillSheet.Target>.Type, target), $"{target}");

            SkillSheet.Position pos = (SkillSheet.Position)p;
            Assert.IsTrue(Enum.IsDefined(VvrTypeHelper.TypeOf<SkillSheet.Position>.Type, pos), $"{target}");

            m_Target   = target;
            m_Position = pos;
        }
    }
}