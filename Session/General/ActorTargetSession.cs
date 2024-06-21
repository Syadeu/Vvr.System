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
// File created : 2024, 06, 21 19:06

#endregion

using System;
using System.Buffers;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Provider;
using Vvr.Controller.Skill;
using Vvr.Model;
using Vvr.Session.Actor;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class ActorTargetSession : ChildSession<ActorTargetSession.SessionData>,
        ITargetProvider
    {
        public struct SessionData : ISessionData
        {
            [NotNull] public IStageActorField UserField { get; }
            [NotNull] public IStageActorField EnemyField { get; }

            public SessionData([NotNull] IStageActorField u, [NotNull] IStageActorField e)
            {
                Assert.IsNotNull(u);
                Assert.IsNotNull(e);

                UserField  = u;
                EnemyField = e;
            }
        }

        public override string DisplayName => nameof(ActorTargetSession);

        [NotNull]
        private IStageActorField GetAllyField([NotNull] IActor actor)
        {
            var field = actor.Owner == Owner ? Data.UserField : Data.EnemyField;
            return field;
        }
        [NotNull]
        private IStageActorField GetEnemyField([NotNull] IActor actor)
        {
            var field = actor.Owner == Owner ? Data.EnemyField : Data.UserField;
            return field;
        }

        public IEnumerable<IActor> FindTargets(IActor from, ITargetDefinition target)
        {
              if (target.Target                            == 0 ||
                (target.Target & SkillSheet.Target.Self) == SkillSheet.Target.Self)
            {
                Assert.IsFalse(from.Disposed);
                yield return from;

                if (target.Target == 0) yield break;
            }

            if ((target.Target & SkillSheet.Target.Ally) == SkillSheet.Target.Ally)
            {
                "target is ally".ToLog();
                var field = GetAllyField(from);
                int count = field.Count;

                using var cachedArray = TempArray<IStageActor>.Shared(count);
                field.CopyToWithTargetPriority(cachedArray.Value);

                SkillSheet.Position targetPosition = target.Position;
                if (targetPosition == SkillSheet.Position.Random)
                {
                    var rnd = Unity.Mathematics.Random.CreateFromIndex(FNV1a32.Calculate(Guid.NewGuid()));
                    cachedArray.Value.Shuffle(ref rnd, count);
                }

                bool targetFound = false;
                foreach (var actor in GetTargets(cachedArray.Value, count, field, targetPosition))
                {
                    targetFound = true;
                    yield return actor.Owner;
                }

                // If there is no matching target, just feed all targets.
                if (!targetFound && count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        yield return cachedArray.Value[i].Owner;
                    }
                }
            }

            if ((target.Target & SkillSheet.Target.Enemy) == SkillSheet.Target.Enemy)
            {
                "target is enemy".ToLog();
                var field = GetEnemyField(from);
                int count = field.Count;

                using var cachedArray = TempArray<IStageActor>.Shared(count);
                field.CopyToWithTargetPriority(cachedArray.Value);

                SkillSheet.Position targetPosition = target.Position;
                if (targetPosition == SkillSheet.Position.Random)
                {
                    var rnd = Unity.Mathematics.Random.CreateFromIndex(FNV1a32.Calculate(Guid.NewGuid()));
                    cachedArray.Value.Shuffle(ref rnd, count);
                }

                bool targetFound = false;
                foreach (var actor in GetTargets(cachedArray.Value, count, field, targetPosition))
                {
                    targetFound = true;
                    yield return actor.Owner;
                }

                // If there is no matching target, just feed all targets.
                if (!targetFound && count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        yield return cachedArray.Value[i].Owner;
                    }
                }
            }
        }

        private static IEnumerable<IStageActor> GetTargets(
            IList<IStageActor> list, int count, IStageActorField field, SkillSheet.Position targetPosition)
        {
            for (var i = 0; i < count; i++)
            {
                var actor = list[i];
                Assert.IsFalse(actor.Owner.Disposed);
                bool isFront = field.ResolvePosition(actor);

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

                yield return actor;
            }
        }
    }
}