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
// File created : 2024, 05, 19 17:05
#endregion

using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Vvr.Controller;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.World;

namespace Vvr.System.SkillCreator
{
    public sealed class SkillCreatorSystem : MonoBehaviour
    {
        private DefaultWorld m_World;

        private async UniTaskVoid Start()
        {
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }

            m_World = await GameWorld.GetOrCreate<DefaultWorld>(Owner.Issue);
            await m_World.CreateSession<SkillTestUserSession>(default);

            m_World.DefaultMap.CreateSession<DefaultRegion>(default).Forget();
            "end".ToLog();
        }

        public void TimeUpdate()
        {
            TimeController.Next(1).Forget();
        }
    }
}