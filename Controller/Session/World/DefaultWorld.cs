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
using Vvr.Provider;

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

        private readonly Dictionary<Hash, int>            m_ExecutionCount = new();
        private          IEnumerable<GameConfigSheet.Row> m_Configs;

        public override string DisplayName => nameof(DefaultWorld);

        public IActorProvider ActorProvider => m_ActorProvider;

        protected override async UniTask OnInitialize(IParentSession session, RootData data)
        {
            m_ActorProvider = new();

            Vvr.Provider.Provider.Static.Connect<IStateConditionProvider>(this);
            Vvr.Provider.Provider.Static.Connect<IGameConfigProvider>(this);

            TimeController.Register(this);

            ConditionTrigger.OnEventExecutedAsync += OnEventExecutedAsync;

            Register(ActorProvider);

            // TODO: skip map load
            DefaultMap = await CreateSession<DefaultMap>(default);
        }
        protected override UniTask OnReserve()
        {
            Unregister<IActorProvider>();

            m_ActorProvider.Dispose();

            Vvr.Provider.Provider.Static.Disconnect<IStateConditionProvider>(this);
            Vvr.Provider.Provider.Static.Disconnect<IGameConfigProvider>(this);

            TimeController.Unregister(this);

            ConditionTrigger.OnEventExecutedAsync -= OnEventExecutedAsync;

            return base.OnReserve();
        }

        private async UniTask OnEventExecutedAsync(
            IEventTarget e, Model.Condition condition, string value)
        {
            Assert.IsFalse(e.Disposed);

            // TODO: temp
            if (e is not IActor target) return;

            foreach (var config in m_Configs)
            {
                // Prevent infinite loop
                await UniTask.Yield();

                if (!EvaluateActorConfig(config, target, out int executedCount)) continue;

                $"[World] execute config : {target.DisplayName} : {condition}, {value}".ToLog();

                m_ExecutionCount[target.GetHash()] = ++executedCount;
                await ExecuteMethod(target, config.Execution.Method, config.Parameters);
            }
        }

        private bool EvaluateActorConfig(GameConfigSheet.Row config, IActor target, out int executedCount)
        {
            Assert.IsNotNull(config);
            executedCount = 0;

            // Check lifecycle condition
            if (config.Lifecycle.Condition != 0)
            {
                Assert.IsNotNull(m_StateProvider, "m_StateProvider != null");

                if (!m_StateProvider.Resolve(config.Lifecycle.Condition, target, config.Lifecycle.Value))
                    return false;
            }

            Assert.IsFalse(target.Disposed);
            Assert.IsNotNull(target.ConditionResolver);
            if (!target.ConditionResolver[(Model.Condition)config.Evaluation.Condition](config.Evaluation.Value))
                return false;

            // $"[World] Evaluation completed {condition} == {config.Evaluation.Condition}".ToLog();

            if (!target.ConditionResolver[config.Execution.Condition](config.Execution.Value))
                return false;

            // $"[World] Execution condition completed {condition} == {config.Execution.Condition}".ToLog();

            // Check probability
            if (!ProbabilityResolver.Get().Resolve(config.Evaluation.Probability))
            {
                return false;
            }

            Hash hash = target.GetHash();
            m_ExecutionCount.TryGetValue(hash, out executedCount);

            // Exceed max execution count
            if (0 > config.Evaluation.MaxCount) return true;

            return config.Evaluation.MaxCount >= executedCount + 1;
        }

        void IConnector<IStateConditionProvider>.Connect(IStateConditionProvider t)
        {
            m_StateProvider = t;
        }

        void IConnector<IStateConditionProvider>.Disconnect()
        {
            m_StateProvider = null;
        }

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
        private async UniTask ExecuteMethod(IEventTarget o, Model.GameMethod method, IReadOnlyList<string> parameters)
        {
            // var methodProvider = await Vvr.Provider.Provider.Static.GetAsync<IGameMethodProvider>();
            var methodProvider = GetProvider<IGameMethodProvider>();
            await methodProvider.Resolve(method)(o, parameters);
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