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
    public sealed class ActorInputProviderComponent : MonoBehaviour, IManualInputProvider
    {
        private readonly LinkedList<UniTask> m_Tasks = new();

        private IActor     m_Target;
        private IActorData m_Data;
        private bool       m_Pass;

        public bool HasControl { get; private set; }

        private void OnEnable()
        {
            Vvr.Provider.Provider.Static.Register<IManualInputProvider>(this);
        }
        private void OnDisable()
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
        }

        public void SetAuto(bool auto)
        {
            if (enabled == !auto) return;

            enabled = !auto;
        }

        public void SetPass()
        {
            if (!HasControl) return;

            m_Pass = true;
        }

        public void ExecuteSkill(int index)
        {
            if (!HasControl) return;

            var skill = m_Data.Skills[index];
            m_Target.Skill.Queue(skill);
        }
    }
}