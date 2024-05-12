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
using System.Buffers;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    partial class DefaultStage : ITargetProvider
    {
        IEnumerable<IActor> ITargetProvider.FindTargets(
            IActor from, ITargetDefinition target)
        {
            if (target.Target                            == 0 ||
                (target.Target & SkillSheet.Target.Self) == SkillSheet.Target.Self)
            {
                Assert.IsFalse(from.Disposed);
                yield return from;

                if (target.Target == 0) yield break;
            }

            var rnd = Unity.Mathematics.Random.CreateFromIndex(FNV1a32.Calculate(Guid.NewGuid()));
            if ((target.Target & SkillSheet.Target.Ally) == SkillSheet.Target.Ally)
            {
                "target is ally".ToLog();
                var field       = m_EnemyId != from.Owner ? m_PlayerField : m_EnemyField;
                int     count       = field.Count;
                var     cachedArray = ArrayPool<IRuntimeActor>.Shared.Rent(count);
                field.CopyTo(cachedArray);
                if (target.Position == SkillSheet.Position.Random)
                {
                    cachedArray.Shuffle(ref rnd, count);
                }

                for (int i = 0; i < count; i++)
                {
                    var actorData = cachedArray[i];
                    Assert.IsFalse(actorData.Owner.Disposed);

                    bool isFront = ResolvePosition(field, actorData);

                    SkillSheet.Position targetPosition = target.Position;
                    if (targetPosition != 0)
                    {
                        if ((targetPosition & SkillSheet.Position.Forward) == SkillSheet.Position.Forward &&
                            !isFront)
                        {
                            continue;
                        }
                        if ((targetPosition & SkillSheet.Position.Backward) == SkillSheet.Position.Backward &&
                            isFront)
                        {
                            continue;
                        }
                    }

                    yield return actorData.Owner;
                }

                ArrayPool<IRuntimeActor>.Shared.Return(cachedArray, true);
            }

            if ((target.Target & SkillSheet.Target.Enemy) == SkillSheet.Target.Enemy)
            {
                "target is enemy".ToLog();
                var field = m_EnemyId != from.Owner ? m_EnemyField : m_PlayerField;
                int count       = field.Count;
                var cachedArray = ArrayPool<IRuntimeActor>.Shared.Rent(count);
                field.CopyTo(cachedArray);

                for (int i = 0; i < count; i++)
                {
                    var actorData = cachedArray[i];
                    Assert.IsFalse(actorData.Owner.Disposed);
                    yield return actorData.Owner;
                }

                ArrayPool<IRuntimeActor>.Shared.Return(cachedArray, true);
            }
        }
    }

}