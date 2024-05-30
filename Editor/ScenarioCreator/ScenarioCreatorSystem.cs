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
// File created : 2024, 05, 21 08:05
#endregion

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.World;
using Vvr.TestClass;

namespace Vvr.ScenarioCreator
{
    public class ScenarioCreatorSystem : TestSystem<TestWorld>
    {
        [Space] [SerializeField] private DialogueData m_DialogueData;

        [Space]
        [SerializeField] private string[]      m_Actors;
        [SerializeField] private string        m_CurrentStageId;
        [SerializeField] private TestStageData m_StageData;

        protected override async UniTask OnStart(TestWorld world)
        {
            if (m_CurrentStageId.IsNullOrEmpty())
            {
                m_StageData.Build(world.DataSession.SheetContainer);
                await world.CreateSession<FakeUserSession>(
                    new FakeUserSession.SessionData(m_Actors, m_StageData));
            }
            else
            {
                await world.CreateSession<FakeUserSession>(
                    new FakeUserSession.SessionData(m_Actors, m_CurrentStageId));
            }

            // world.DefaultMap.CreateSession<DefaultRegion>(default).Forget();

            if (m_DialogueData != null)
            {
                IDialoguePlayProvider dialoguePlayProvider = world.GetProviderRecursive<IDialoguePlayProvider>();
                Assert.IsNotNull(dialoguePlayProvider);
                dialoguePlayProvider.Play(m_DialogueData).Forget();
            }
        }

        public void TimeUpdate()
        {
            TimeController.Next(1).Forget();
        }
    }
}