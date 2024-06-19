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
// File created : 2024, 05, 21 09:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.BehaviorTree;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public class GameMethodResolveSession : ParentSession<GameMethodResolveSession.SessionData>,
        IGameMethodProvider,
        IConnector<IDialoguePlayProvider>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(GameMethodResolveSession);

        private IDialoguePlayProvider m_DialoguePlayProvider;

        GameMethodImplDelegate IGameMethodProvider.Resolve(Model.GameMethod method)
        {
            using var timer = DebugTimer.Start();

            if (method == Model.GameMethod.Destroy)
            {
                return GameMethod_Destroy;
            }

            if (method == Model.GameMethod.ExecuteBehaviorTree)
            {
                return GameMethod_ExecuteBehaviorTree;
            }

            if (method == GameMethod.ExecuteDialogue)
            {
                return GameMethod_ExecuteDialogue;
            }

            throw new NotImplementedException();
        }

        private async UniTask GameMethod_ExecuteDialogue(IEventTarget e, IReadOnlyList<string> parameters)
        {
            await m_DialoguePlayProvider.Play(parameters[0]);
        }

        private async UniTask GameMethod_Destroy(IEventTarget e, IReadOnlyList<string> parameters)
        {
            Assert.IsFalse(e.Disposed);

            if (e is IActor x)
            {
                await DestroyActor(x);
                return;
            }

            throw new NotImplementedException($"{e.GetType().FullName}({e.DisplayName}) has no destroy method");
        }

        private async UniTask DestroyActor(IActor x)
        {
            IStageInfoProvider stageInfo = Parent.GetProviderRecursive<IStageInfoProvider>();
            Assert.IsNotNull(stageInfo);

            using (var trigger = ConditionTrigger.Push(x, nameof(Model.GameMethod)))
            {
                await trigger.Execute(Model.Condition.OnBattleEnd, null);
                await trigger.Execute(Model.Condition.OnActorDead, null);
            }

            await stageInfo.Delete(x);
        }

        async UniTask GameMethod_ExecuteBehaviorTree(IEventTarget e, IReadOnlyList<string> parameters)
        {
            if (e is not IBehaviorTarget b) throw new InvalidOperationException();

            await b.Execute(parameters);
        }

        void IConnector<IDialoguePlayProvider>.Connect(IDialoguePlayProvider    t) => m_DialoguePlayProvider = t;
        void IConnector<IDialoguePlayProvider>.Disconnect(IDialoguePlayProvider t) => m_DialoguePlayProvider = null;
    }
}