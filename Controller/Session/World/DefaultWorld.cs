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
using Vvr.System.Model;
using Vvr.System.Provider;

namespace Vvr.System.Controller
{
    [Preserve]
    public partial class DefaultWorld : RootSession, IWorldSession,
        IConnector<IStateConditionProvider>,
        IConnector<IGameConfigProvider>
    {
        public DefaultMap     DefaultMap { get; private set; }
        public ConditionQuery Filter     { get; }

        private IGameConfigProvider      m_ConfigProvider;
        private IStateConditionProvider m_StateProvider;

        protected override async UniTask OnInitialize()
        {
            Provider.Provider.Static.Connect<IStateConditionProvider>(this);
            Provider.Provider.Static.Connect<IGameConfigProvider>(this);

            TimeController.Register(this);

            ConditionTrigger.OnEventExecutedAsync += OnEventExecutedAsync;

            // TODO: skip map load
            DefaultMap = await CreateSession<DefaultMap>(default);
        }
        protected override UniTask OnReserve()
        {
            Provider.Provider.Static.Disconnect<IStateConditionProvider>(this);
            Provider.Provider.Static.Disconnect<IGameConfigProvider>(this);

            TimeController.Unregister(this);

            ConditionTrigger.OnEventExecutedAsync -= OnEventExecutedAsync;

            return base.OnReserve();
        }

        private async UniTask OnEventExecutedAsync(
            IEventTarget e, Condition condition, string value)
        {
            // TODO: temp
            if (e is not IActor target) return;

            $"[World] Update configs :{target} :{condition}: {m_Configs.Count()}".ToLog();
            foreach (var config in m_Configs)
            {
                // Check lifecycle condition
                if (config.Lifecycle.Condition != 0)
                {
                    Assert.IsNotNull(m_StateProvider, "m_StateProvider != null");

                    if (!m_StateProvider.Resolve(config.Lifecycle.Condition, target, config.Lifecycle.Value))
                        continue;
                }

                if (!target.ConditionResolver[(Condition)config.Evaluation.Condition](config.Evaluation.Value)) continue;

                $"Evaluation completed {condition} == {config.Evaluation.Condition}".ToLog();

                if (!target.ConditionResolver[config.Execution.Condition](config.Execution.Value))
                    continue;

                $"Execution condition completed {condition} == {config.Execution.Condition}".ToLog();

                // Check probability
                if (!ProbabilityResolver.Get().Resolve(config.Evaluation.Probability))
                {
                    continue;
                }

                Hash hash = new Hash(config.Id);
                m_ExecutionCount.TryGetValue(hash, out int count);

                // Exceed max execution count
                if (0 <= config.Evaluation.MaxCount)
                {
                    if (config.Evaluation.MaxCount < count + 1)
                    {
                        continue;
                    }
                }

                await ExecuteMethod(target, config.Execution.Method);

                m_ExecutionCount[hash] = ++count;
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
        private async UniTask ExecuteMethod(IEventTarget o, GameMethod method)
        {
            var methodProvider = await Provider.Provider.Static.GetAsync<IGameMethodProvider>();
            await methodProvider.Resolve(method)(o);
        }

        async UniTask ITimeUpdate.OnUpdateTime(int currentTime, int deltaTime)
        {

        }
        async UniTask ITimeUpdate.OnEndUpdateTime()
        {
        }
    }
}