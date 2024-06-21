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
// File created : 2024, 05, 10 22:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;

namespace Vvr.Session.World
{
    partial class DefaultStage : IStateConditionProvider, IEventConditionProvider
    {
        bool IStateConditionProvider.Resolve(StateCondition condition, IEventTarget target, string value)
        {
            switch (condition)
            {
                case StateCondition.Always: return true;
                case StateCondition.IsActorTurn:
                    if (m_Timeline[0].Owner == target) return true;

                    if (m_Timeline[0].Owner.Owner == target.Owner &&
                        m_HandActors.Any<IStageActor>(x => ReferenceEquals(x.Owner, target)))
                    {
                        return true;
                    }

                    return false;
                case StateCondition.IsInHand:
                    if (target.Owner == m_EnemyId) return false;

                    return m_HandActors.Any<IStageActor>(x => ReferenceEquals(x.Owner, target));
                case StateCondition.IsPlayerActor:
                    return target.Owner != m_EnemyId;
                case StateCondition.IsFront:
                    if (m_HandActors.Any(x => ReferenceEquals(x.Owner, target)))
                    {
                        "Hand actor always false".ToLogError();
                        return false;
                    }

                    StageActorFieldSession field;
                    if (target.Owner == m_EnemyId)
                    {
                        field = m_EnemyField;
                    }
                    else field = m_PlayerField;

                    return field.ResolvePosition(field.First(x => ReferenceEquals(x.Owner, target)));
                case StateCondition.IsParrying:
                    return ConditionTrigger.Last(target, Condition.OnParrying);
                    break;
                case StateCondition.CanTag:
                    // TODO: Cool time
                    if (target.Owner != Owner) return false;

                    // If already tagged
                    if (m_PlayerField.Count > 1) return false;
                    return true;
                case StateCondition.CanParry:
                    var stageActor = m_StageActorProvider.Get((IActor)target);
                    Assert.IsNotNull(stageActor);

                    return stageActor.ParryCount > 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }
        }
        bool IEventConditionProvider.Resolve(EventCondition condition, IEventTarget target, string value)
        {
            if (condition == 0) throw new InvalidOperationException();

            return ConditionTrigger.Any(target, (Condition)condition, value);
        }
    }
}