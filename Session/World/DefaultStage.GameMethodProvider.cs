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
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.BehaviorTree;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.Session.Actor;

namespace Vvr.Session.World
{
    partial class DefaultStage : IGameMethodProvider
    {
        // private bool m_DestroyProcessing;

        GameMethodImplDelegate IGameMethodProvider.Resolve(Model.GameMethod method)
        {
            if (method == Model.GameMethod.Destroy)
            {
                return GameMethod_Destroy;
            }

            if (method == Model.GameMethod.ExecuteBehaviorTree)
            {
                return GameMethod_ExecuteBehaviorTree;
            }

            throw new NotImplementedException();
        }

        private async UniTask GameMethod_Destroy(IEventTarget e, IReadOnlyList<string> parameters)
        {
            if (e is not IActor x) return;

            // m_DestroyProcessing = true;
            using (var trigger = ConditionTrigger.Push(x, nameof(Model.GameMethod)))
            {
                await trigger.Execute(Model.Condition.OnBattleEnd, null);
                await trigger.Execute(Model.Condition.OnActorDead, null);
            }

            var field = x.ConditionResolver[Model.Condition.IsPlayerActor](null) ? m_PlayerField : m_EnemyField;
            int index = field.FindIndex(e => e.owner == x);
            if (index < 0)
            {
                $"{index} not found in field {x.ConditionResolver[Model.Condition.IsPlayerActor](null)}".ToLogError();
                return;
            }

            StageActor actor = field[index];

            actor.owner.DisconnectTime();

            Disconnect<IActorDataProvider>(actor.owner.Skill);
            Disconnect<ITargetProvider>(actor.owner.Skill);
            Disconnect<ITargetProvider>(actor.owner.Passive);

            $"Actor {actor.owner.DisplayName} is dead {actor.owner.Stats[StatType.HP]}".ToLog();

            Assert.IsFalse(Disposed);
            await Delete(field, actor);
        }

        async UniTask GameMethod_ExecuteBehaviorTree(IEventTarget e, IReadOnlyList<string> parameters)
        {
            if (e is not IBehaviorTarget b) throw new InvalidOperationException();

            await b.Execute(parameters);
        }
    }
}