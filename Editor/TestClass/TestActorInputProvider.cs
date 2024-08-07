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
// File created : 2024, 05, 19 20:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Controller;
using Vvr.Model;
using Vvr.Session.Input;

namespace Vvr.TestClass
{
    [Obsolete("", true)]
    class TestActorInputProvider : ActorInputProviderComponent
    {
        [SerializeField] private bool m_SkipSkillCooltime;

        protected override void OnSkillExecuted(ISkillData skill)
        {
            if (m_SkipSkillCooltime)
                SkipTime(skill.Cooltime).Forget();

            base.OnSkillExecuted(skill);
        }

        private async UniTaskVoid SkipTime(float time)
        {
            await UniTask.WaitForSeconds(0.1f);

            await TimeController.Next(time);
        }
    }
}