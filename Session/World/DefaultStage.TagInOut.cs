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
// File created : 2024, 05, 18 17:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
    partial class DefaultStage : IStageActorTagInOutProvider
    {
        async UniTask IStageActorTagInOutProvider.TagIn(IActor actor)
        {
            Assert.IsNotNull(actor);

            int index = m_HandActors.FindIndex(x => ReferenceEquals(x.Owner, actor));
            if (index < 0)
            {
                "This actor is not in hand".ToLogError();
                return;
            }

            await TagIn(index);
        }

        private partial async UniTask TagIn(int index)
        {
            // Assert.IsTrue(m_Timeline.First.Value.actor.ConditionResolver[Model.Condition.IsPlayerActor](null));
            if (m_PlayerField.Count > 1)
            {
                "Cant swap. already in progress".ToLog();
                return;
            }

            Assert.IsFalse(index < 0);
            Assert.IsTrue(index  < m_HandActors.Count);

            var temp = m_HandActors[index];
            m_HandActors.RemoveAt(index);

            if (m_PlayerField.Count > 0)
            {
                IStageActor currentFieldRuntimeActor = m_PlayerField[0];
                currentFieldRuntimeActor.TagOutRequested = true;

                await JoinAfter(currentFieldRuntimeActor, m_PlayerField, temp);

                m_TimelineQueueProvider.SetEnable(currentFieldRuntimeActor, false);
                await RemoveFromTimeline(currentFieldRuntimeActor, 1);

                await m_ViewProvider.CardViewProvider.Resolve(currentFieldRuntimeActor.Owner);
            }
            else
            {
                await Join(m_PlayerField, temp);
            }

            UpdateTimeline();

            await m_ViewProvider.CardViewProvider.Resolve(temp.Owner);
            using (var trigger = ConditionTrigger.Push(temp.Owner, ConditionTrigger.Game))
            {
                await trigger.Execute(Model.Condition.OnTagIn, temp.Owner.Id);
            }
        }
    }
}