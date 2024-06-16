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
// File created : 2024, 05, 15 19:05
#endregion

using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Provider;

namespace Vvr.Controller.BehaviorTree
{
    public sealed class BehaviorTreeController : IDisposable
    {
        private readonly IBehaviorTarget m_Owner;

        private AsyncLazy<IEventTargetViewProvider> m_ViewProvider;

        public BehaviorTreeController(IBehaviorTarget owner)
        {
            m_Owner        = owner;
            m_ViewProvider = Vvr.Provider.Provider.Static.GetLazyAsync<IEventTargetViewProvider>();
        }
        public void Dispose()
        {
            m_ViewProvider = null;
        }

        public async UniTask StartBehavior(ExternalBehavior behavior)
        {
            var viewProvider = await m_ViewProvider;

            var view = await viewProvider.Resolve(m_Owner);

            var ctr = view.GetOrAddComponent<Behavior>();
            Assert.IsFalse(ctr.ExecutionStatus == TaskStatus.Running);

            ctr.DisableBehavior();
            ctr.ExternalBehavior = behavior;
            ctr.EnableBehavior();

            while (ctr.ExecutionStatus == TaskStatus.Running)
            {
                await UniTask.Yield();
            }

            ctr.DisableBehavior();
            ctr.ExternalBehavior = null;
        }
    }

    public interface IBehaviorTarget : IEventTarget
    {
        UniTask Execute(IReadOnlyList<string> parameters);
    }
}