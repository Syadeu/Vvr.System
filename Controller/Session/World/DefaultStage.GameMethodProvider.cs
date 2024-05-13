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
// File created : 2024, 05, 13 01:05

#endregion

using System;
using System.Collections.Generic;
using Vvr.System.Model;
using Vvr.UI.Observer;

namespace Vvr.System.Controller
{
    partial class DefaultStage : IGameMethodProvider
    {
        // private bool m_DestroyProcessing;

        GameMethodImplDelegate IGameMethodProvider.Resolve(GameMethod method)
        {
            if (method == GameMethod.Destroy)
            {
                return async e =>
                {
                    // if (m_DestroyProcessing) return;
                    if (e is not IActor x) return;

                    // m_DestroyProcessing = true;
                    using (var trigger = ConditionTrigger.Push(x, nameof(GameMethod)))
                    {
                        await trigger.Execute(Condition.OnActorDead, null);
                    }

                    var field = x.ConditionResolver[Condition.IsPlayerActor](null) ? m_PlayerField : m_EnemyField;
                    int index = field.FindIndex(e => e.owner == x);
                    if (index < 0)
                    {
                        $"{index} not found in field {x.ConditionResolver[Condition.IsPlayerActor](null)}".ToLogError();
                        return;
                    }

                    RuntimeActor actor = field[index];

                    $"Actor {actor.owner.DisplayName} is dead {actor.owner.Stats[StatType.HP]}".ToLog();

                    await Delete(field, actor);

                    // m_Timeline.Clear();
                    // AddActorsInOrderWithSpeed(5);
                    // ObjectObserver<ActorList>.ChangedEvent(m_Timeline);

                    // m_DestroyProcessing = false;
                };
            }

            throw new NotImplementedException();
        }
    }
}