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

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Session.World
{
    [Preserve]
    public partial class DefaultWorld : RootSession, IWorldSession,
        IConnector<IStateConditionProvider>,
        IConnector<IGameConfigProvider>
    {
        public DefaultMap     DefaultMap { get; private set; }

        private IGameConfigProvider      m_ConfigProvider;
        private IStateConditionProvider m_StateProvider;

        private ActorProvider m_ActorProvider;

        public override string DisplayName => nameof(DefaultWorld);

        public IActorProvider ActorProvider => m_ActorProvider;

        protected override async UniTask OnInitialize()
        {
            m_ActorProvider = new();

            MPC.Provider.Provider.Static.Connect<IStateConditionProvider>(this);
            MPC.Provider.Provider.Static.Connect<IGameConfigProvider>(this);

            TimeController.Register(this);

            ConditionTrigger.OnEventExecutedAsync += OnEventExecutedAsync;

            Connect(ActorProvider);

            // TODO: skip map load
            DefaultMap = await CreateSession<DefaultMap>(default);
        }
        protected override UniTask OnReserve()
        {
            Disconnect<IActorProvider>();

            m_ActorProvider.Dispose();

            MPC.Provider.Provider.Static.Disconnect<IStateConditionProvider>(this);
            MPC.Provider.Provider.Static.Disconnect<IGameConfigProvider>(this);

            TimeController.Unregister(this);

            ConditionTrigger.OnEventExecutedAsync -= OnEventExecutedAsync;

            return base.OnReserve();
        }

        private async UniTask OnEventExecutedAsync(
            IEventTarget e, Model.Condition condition, string value)
        {
            // TODO: temp
            if (e is not IActor target) return;

            $"[World] Update configs :{target} :{condition}: {m_Configs.Count()}".ToLog();
            foreach (var config in m_Configs)
            {
                // Prevent infinite loop
                await UniTask.Yield();

                // Check lifecycle condition
                if (config.Lifecycle.Condition != 0)
                {
                    Assert.IsNotNull(m_StateProvider, "m_StateProvider != null");

                    if (!m_StateProvider.Resolve(config.Lifecycle.Condition, target, config.Lifecycle.Value))
                        continue;
                }

                if (!target.ConditionResolver[(Model.Condition)config.Evaluation.Condition](config.Evaluation.Value)) continue;

                $"[World] Evaluation completed {condition} == {config.Evaluation.Condition}".ToLog();

                if (!target.ConditionResolver[config.Execution.Condition](config.Execution.Value))
                    continue;

                $"[World] Execution condition completed {condition} == {config.Execution.Condition}".ToLog();

                // Check probability
                if (!ProbabilityResolver.Get().Resolve(config.Evaluation.Probability))
                {
                    continue;
                }

                Hash hash = e.GetHash();
                m_ExecutionCount.TryGetValue(hash, out int count);

                // Exceed max execution count
                if (0 <= config.Evaluation.MaxCount)
                {
                    if (config.Evaluation.MaxCount < count + 1)
                    {
                        continue;
                    }

                    m_ExecutionCount[hash] = ++count;
                }

                await ExecuteMethod(target, config.Execution.Method);
            }
        }

        void IConnector<IStateConditionProvider>.Connect(IStateConditionProvider t)
        {
            m_StateProvider = t;
        }

        void IConnector<IStateConditionProvider>.Disconnect()
        {
            m_StateProvider = null;
        }

        private readonly Dictionary<Hash, int>            m_ExecutionCount = new();
        private          IEnumerable<GameConfigSheet.Row> m_Configs;

        void IConnector<IGameConfigProvider>.Connect(IGameConfigProvider t)
        {
            m_ConfigProvider = t;
            m_Configs = m_ConfigProvider[MapType.Global];
        }
        void IConnector<IGameConfigProvider>.Disconnect()
        {
            m_ConfigProvider = null;
        }
    }
    partial class DefaultWorld : ITimeUpdate
    {
        private async UniTask ExecuteMethod(IEventTarget o, Model.GameMethod method)
        {
            var methodProvider = await MPC.Provider.Provider.Static.GetAsync<IGameMethodProvider>();
            await methodProvider.Resolve(method)(o);
        }

        async UniTask ITimeUpdate.OnUpdateTime(int currentTime, int deltaTime)
        {

        }
        async UniTask ITimeUpdate.OnEndUpdateTime()
        {
            m_ExecutionCount.Clear();
        }
    }
}