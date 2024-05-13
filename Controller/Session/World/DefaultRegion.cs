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
// File created : 2024, 05, 10 20:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Vvr.MPC.Provider;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    [ParentSession(typeof(DefaultMap))]
    public class DefaultRegion : ParentSession<DefaultRegion.SessionData>, ISessionTarget
    {
        public struct SessionData : ISessionData
        {
            public readonly StageSheet sheet;
            public readonly string     startStageId;

            public SessionData(StageSheet t, string ta)
            {
                sheet        = t;
                startStageId = ta;
            }
        }

        private ConditionResolver m_ConditionResolver;

        public Owner                              Owner             { get; private set; }
        string IEventTarget.                      DisplayName       => nameof(DefaultRegion);
        public bool                               Disposed          { get; private set; }
        IReadOnlyConditionResolver ISessionTarget.ConditionResolver => m_ConditionResolver;


        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Owner = Owner.Issue;

            m_ConditionResolver = ConditionResolver.Create(this);

            Disposed = false;

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_ConditionResolver.Dispose();

            Disposed            = true;
            m_ConditionResolver = null;
            return base.OnReserve();
        }

        public async UniTask Start(StageSheet sheet, Owner playerId, ActorSheet.Row[] playerData)
        {
            // StageSheet.Row startStage = Data.sheet[Data.startStageId];
            StageSheet.Row startStage = sheet[0];

            StageSheet.Row             currentStage = startStage;
            LinkedList<StageSheet.Row> list         = new();

            CachedActor[] aliveActors = Array.Empty<CachedActor>();
            do
            {
                list.AddLast(currentStage);
                if (currentStage.IsFinalStage)
                {
                    var floor = await CreateSession<DefaultFloor>(new DefaultFloor.SessionData(list, aliveActors));

                    DefaultFloor.Result result = await floor.Start(playerId, playerData);
                    aliveActors = result.alivePlayerActors;

                    await floor.Reserve();

                    "Floor cleared".ToLog();
                    list.Clear();

                    if (aliveActors.Length <= 0)
                    {
                        "All player is dead reset".ToLog();
                        currentStage = startStage;
                    }
                }

                currentStage = currentStage.NextStage.Ref;
                await UniTask.Yield();
            } while (currentStage != null);
        }
    }

    public interface ISessionTarget : IEventTarget
    {
        IReadOnlyConditionResolver ConditionResolver { get; }
    }
}