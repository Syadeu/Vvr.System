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
// File created : 2024, 05, 18 11:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Controller.Actor;
using Vvr.Model;

namespace Vvr.Session.Input
{
    [DisallowMultipleComponent]
    [Obsolete("", true)]
    public class ActorInputProviderComponent : MonoBehaviour, IManualInputProvider
    {
        private readonly LinkedList<UniTask> m_Tasks = new();

        private IActor     m_Target;
        private IActorData m_Data;
        private bool       m_Pass;

        private bool m_IsSkillExecuting;

        public bool HasControl { get; private set; }

        protected virtual void OnEnable()
        {
            Vvr.Provider.Provider.Static.Register<IManualInputProvider>(this);
        }

        protected virtual void OnDisable()
        {
            Vvr.Provider.Provider.Static.Unregister<IManualInputProvider>(this);
        }

        public async UniTask OnControl(IActor target, IActorData data)
        {
            HasControl = true;
            m_Target   = target;
            m_Data     = data;

            while (!m_Pass)
            {
                await UniTask.Yield();
            }

            m_Pass = false;

            m_Target   = null;
            m_Data     = null;
            HasControl = false;

            while (m_IsSkillExecuting)
            {
                await UniTask.Yield();
            }
        }

        public void SetAuto(bool auto)
        {
            if (enabled == !auto) return;

            enabled = !auto;
        }

        public void SetPass()
        {
            if (!HasControl || m_Data == null) return;

            m_Pass = true;

            HasControl = false;
            m_Data     = null;
        }

        public void ExecuteSkill(int index)
        {
            if (!HasControl || m_Data == null) return;

            if (m_IsSkillExecuting)
            {
                "Already executing".ToLog();
                return;
            }

            m_IsSkillExecuting = true;

            var skill = m_Data.Skills[index];
            OnExecuteSkill(skill);

            ExecuteSkill(skill).Forget();
        }

        private async UniTask ExecuteSkill(ISkillData skill)
        {
            await m_Target.Skill.QueueAsync(skill);
            OnSkillExecuted(skill);

            m_IsSkillExecuting = false;
        }

        protected virtual void OnExecuteSkill(ISkillData skill)
        {
        }

        protected virtual void OnSkillExecuted(ISkillData skill)
        {

        }
    }
}