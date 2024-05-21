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
// File created : 2024, 05, 17 18:05

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class GameConfigResolveSession : ChildSession<GameConfigResolveSession.SessionData>,
        IGameConfigResolveProvider, ITimeUpdate,
        IConnector<IGameConfigProvider>,
        IConnector<IGameMethodProvider>
    {
        public struct SessionData : ISessionData
        {
            public readonly MapType configType;
            public readonly bool    autoResolve;

            public SessionData(MapType t, bool auto)
            {
                configType  = t;
                autoResolve = auto;
            }
        }

        private IGameConfigProvider m_GameConfigProvider;
        private IGameMethodProvider m_GameMethodProvider;

        private IEnumerable<GameConfigSheet.Row> m_Configs;

        private readonly Dictionary<Hash, int> m_ExecutionCount = new();

        public override string DisplayName => nameof(GameConfigResolveSession);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            m_Configs = m_GameConfigProvider[data.configType];

            if (data.autoResolve)
                ConditionTrigger.OnEventExecutedAsync += Resolve;

            TimeController.Register(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            if (Data.autoResolve)
                ConditionTrigger.OnEventExecutedAsync -= Resolve;

            TimeController.Unregister(this);

            return base.OnReserve();
        }

        public async UniTask Resolve(IEventTarget e, Condition condition, string value)
        {
            Assert.IsFalse(e.Disposed);

            // TODO: temp
            if (e is IConditionTarget target)
            {
                foreach (var config in m_Configs)
                {
                    // Prevent infinite loop
                    await UniTask.Yield();

                    if (!EvaluateConfig(config, target)) continue;
                    if (!EvaluateExecutionCount(config, target, out int executedCount)) continue;

                    $"[World] execute config : {target.DisplayName} : {condition}, {value}".ToLog();

                    m_ExecutionCount[target.GetHash()] = ++executedCount;
                    await ExecuteMethod(
                        config,
                        target, config.Execution.Method, config.Parameters);
                }

                return;
            }
        }

        private bool EvaluateConfig(GameConfigSheet.Row config, IConditionTarget target)
        {
            Assert.IsNotNull(config);

            // Check lifecycle condition
            if (config.Lifecycle.Condition != 0)
            {
                if (!ConditionResolver.CanResolve((Condition)config.Lifecycle.Condition) ||
                    !ConditionResolver[(Condition)config.Lifecycle.Condition](config.Lifecycle.Value))
                {
                    return false;
                }
            }

            Assert.IsFalse(target.Disposed);
            Assert.IsNotNull(target.ConditionResolver);
            if (!target.ConditionResolver.CanResolve((Condition)config.Evaluation.Condition) ||
                !target.ConditionResolver[(Model.Condition)config.Evaluation.Condition](config.Evaluation.Value))
                return false;

            // $"[World] Evaluation completed {condition} == {config.Evaluation.Condition}".ToLog();

            if (!target.ConditionResolver.CanResolve(config.Execution.Condition) ||
                !target.ConditionResolver[config.Execution.Condition](config.Execution.Value))
                return false;

            // $"[World] Execution condition completed {condition} == {config.Execution.Condition}".ToLog();

            // Check probability
            if (!ProbabilityResolver.Get().Resolve(config.Evaluation.Probability))
            {
                return false;
            }

            return true;
        }

        private bool EvaluateExecutionCount(GameConfigSheet.Row config, IEventTarget target, out int executedCount)
        {
            Hash hash = target.GetHash();
            m_ExecutionCount.TryGetValue(hash, out executedCount);

            if (0 > config.Evaluation.MaxCount) return true;

            // Exceed max execution count
            return config.Evaluation.MaxCount > executedCount;
        }

        private async UniTask ExecuteMethod(
            GameConfigSheet.Row config,
            IEventTarget o, Model.GameMethod method, IReadOnlyList<string> parameters)
        {
            using var trigger = ConditionTrigger.Push(this, DisplayName);
            await trigger.Execute(Condition.OnGameConfigStarted, config.Id);
            if (method != 0)
            {
                IGameMethodProvider methodProvider = m_GameMethodProvider;
                // if (methodProvider == null)
                //     methodProvider = Parent.GetProviderRecursive<IGameMethodProvider>();

                Assert.IsNotNull(methodProvider, "methodProvider != null");
                await methodProvider.Resolve(method)(o, parameters);
            }
            await trigger.Execute(Condition.OnGameConfigEnded, config.Id);
        }

        UniTask ITimeUpdate.OnUpdateTime(float currentTime, float deltaTime)
        {
            return UniTask.CompletedTask;
        }
        UniTask ITimeUpdate.OnEndUpdateTime()
        {
            m_ExecutionCount.Clear();
            return UniTask.CompletedTask;
        }

        void IConnector<IGameConfigProvider>.Connect(IGameConfigProvider    t) => m_GameConfigProvider = t;
        void IConnector<IGameConfigProvider>.Disconnect(IGameConfigProvider t) => m_GameConfigProvider = null;
        void IConnector<IGameMethodProvider>.Connect(IGameMethodProvider    t) => m_GameMethodProvider = t;
        void IConnector<IGameMethodProvider>.Disconnect(IGameMethodProvider t) => m_GameMethodProvider = null;
    }
}