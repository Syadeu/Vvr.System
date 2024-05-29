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
// File created : 2024, 05, 30 01:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Provider;
using Vvr.Provider.Command;

namespace Vvr.Session.Actor
{
    [PublicAPI]
    public sealed class SkillExecuteCommand : ICommand
    {
        private readonly int m_SkillIndex;

        public SkillExecuteCommand(int skillIndex)
        {
            m_SkillIndex = skillIndex;
        }

        async UniTask ICommand.ExecuteAsync(IEventTarget target)
        {
            if (target is not IActor actor)
                throw new InvalidOperationException();

            await actor.Skill.Queue(m_SkillIndex);
        }
    }
}