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

using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a session for resolving game configuration.
    /// </summary>
    [UsedImplicitly]
    public sealed class GameConfigResolveSession : ChildSession<GameConfigResolveSession.SessionData>,
        IGameConfigResolveProvider, ITimeUpdate,
        IConnector<IGameConfigProvider>,
        IConnector<IGameMethodProvider>,
        IConnector<IUserDataProvider>
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
        private IUserDataProvider   m_UserDataProvider;

        private IEnumerable<GameConfigSheet.Row> m_Configs;

        private readonly ConcurrentDictionary<IEventTarget, int> m_ExecutionCount = new();
        private readonly Dictionary<string, int> m_LimitCounter   = new();

        public override string DisplayName => nameof(GameConfigResolveSession);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            m_Configs = m_GameConfigProvider[data.configType];
            if (data.autoResolve)
            {
                ConditionTrigger.OnEventExecutedAsync += Resolve;
            }

            TimeController.Register(this);

            return base.OnInitialize(session, data);
        }
        protected override UniTask OnReserve()
        {
            ConditionTrigger.OnEventExecutedAsync -= Resolve;

            TimeController.Unregister(this);

            return base.OnReserve();
        }

        public async UniTask Resolve(IEventTarget e, Condition condition, string value)
        {
            Assert.IsFalse(e.Disposed);

            UniTask task = UniTask.CompletedTask;
            // TODO: temp
            if (e is IConditionTarget target)
            {
                foreach (var config in m_Configs)
                {
                    // Prevent infinite loop
                    await UniTask.Yield();

                    if (config.Lifecycle.Map != Data.configType)
                    {
                        $"??: {config.Lifecycle.Map} != {Data.configType}".ToLogError();
                        continue;
                    }

                    if (config.Evaluation.Condition != condition) continue;

                    if (!EvaluateExecutionCount(config, target, out int executedCount)) continue;
                    if (!EvaluateConfig(config, target)) continue;

                    $"[World] execute config : {target.DisplayName} : {condition}, {value}. {executedCount}".ToLog();

                    // These method must be executed before config method execute.
                    // Config methods also can trigger another config method recursively.
                    m_ExecutionCount[target] = executedCount + 1;
                    $"[World] incre count {m_ExecutionCount[target]}".ToLog();
                    IncrementLimitCounter(config);

                    var t = ExecuteMethod(
                        config,
                        target, config.Execution.Method, config.Parameters);

                    task = UniTask.WhenAll(task, t);
                }

                await task;
            }
        }

        /// <summary>
        /// Evaluates a game configuration.
        /// </summary>
        /// <param name="config">The game configuration to evaluate.</param>
        /// <param name="target">The target to evaluate the configuration against.</param>
        /// <returns>True if the configuration should be executed, false otherwise.</returns>
        private bool EvaluateConfig(GameConfigSheet.Row config, IConditionTarget target)
        {
            const string resolveLifecycle  = "Resolve Lifecycle";
            const string resolveEvaluation = "Resolve Evaluation";
            const string resolveExecution  = "Resolve Execution";

            using var timer = DebugTimer.Start();

            Assert.IsNotNull(config);

            if (!EvaluateLimitedCount(config)) return false;

            // Check lifecycle condition
            if (config.Lifecycle.Condition != 0)
            {
                using (DebugTimer.StartWithCustomName(resolveLifecycle))
                {
                    if (!ConditionResolver.CanResolve((Condition)config.Lifecycle.Condition) ||
                     !ConditionResolver[(Condition)config.Lifecycle.Condition](config.Lifecycle.Value))
                    {
                        return false;
                    }
                }
            }

            Assert.IsFalse(target.Disposed);
            Assert.IsNotNull(target.ConditionResolver);

            using (DebugTimer.StartWithCustomName(resolveEvaluation))
            {
                if (!target.ConditionResolver.CanResolve((Condition)config.Evaluation.Condition) ||
                    !target.ConditionResolver[(Model.Condition)config.Evaluation.Condition](config.Evaluation.Value))
                    return false;
            }

            // $"[World] Evaluation completed {condition} == {config.Evaluation.Condition}".ToLog();

            using (DebugTimer.StartWithCustomName(resolveExecution))
            {
                if (!target.ConditionResolver.CanResolve(config.Execution.Condition) ||
                    !target.ConditionResolver[config.Execution.Condition](config.Execution.Value))
                    return false;
            }

            // $"[World] Execution condition completed {condition} == {config.Execution.Condition}".ToLog();

            // Check probability
            if (!ProbabilityResolver.Get().Resolve(config.Evaluation.Probability))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluates the execution count for a game configuration and target.
        /// </summary>
        /// <param name="config">The game configuration to evaluate.</param>
        /// <param name="target">The target to evaluate the configuration against.</param>
        /// <param name="executedCount">The number of times the target has been executed.</param>
        /// <returns>True if the configuration should be executed, false otherwise.</returns>
        private bool EvaluateExecutionCount(GameConfigSheet.Row config, IEventTarget target, out int executedCount)
        {
            using var timer = DebugTimer.Start();

            m_ExecutionCount.TryGetValue(target, out executedCount);

            if (0 > config.Evaluation.MaxCount) return true;

            // Exceed max execution count
            return config.Evaluation.MaxCount > executedCount;
        }

        /// <summary>
        /// Executes a game configuration method.
        /// </summary>
        /// <param name="config">The game configuration to execute.</param>
        /// <param name="o">The event target to execute the method on.</param>
        /// <param name="method">The method to execute.</param>
        /// <param name="parameters">The parameters to pass to the method.</param>
        /// <returns>A task representing the asynchronous execution of the method.</returns>
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

        /// <summary>
        /// Evaluates whether the given game configuration should be executed, based on the limited count defined in the configuration.
        /// </summary>
        /// <param name="config">The game configuration to evaluate.</param>
        /// <returns>True if the configuration should be executed, false otherwise.</returns>
        private bool EvaluateLimitedCount(GameConfigSheet.Row config)
        {
            using var debugTimer = DebugTimer.Start();
            if (config.Definition.LimitCount < 0) return true;

            int counter;
            if (config.Definition.CacheLimit)
            {
                var key = UserDataPath.GameConfig.ExecutedCount(config.Id);
                counter   = m_UserDataProvider.GetInt(key);
            }
            else
            {
                counter = m_LimitCounter.GetValueOrDefault(config.Id, 0);
            }

            return counter < config.Definition.LimitCount;
        }

        /// <summary>
        /// Increments the limit counter for a game configuration.
        /// </summary>
        /// <param name="config">The game configuration for which to increment the limit counter.</param>
        private void IncrementLimitCounter(GameConfigSheet.Row config)
        {
            if (config.Definition.LimitCount < 0) return;

            if (config.Definition.CacheLimit)
            {
                var key     = UserDataPath.GameConfig.ExecutedCount(config.Id);
                int counter = m_UserDataProvider.GetInt(key);
                m_UserDataProvider.SetInt(key, ++counter);
            }
            else
            {
                int  counter = m_LimitCounter.GetValueOrDefault(config.Id, 0);
                m_LimitCounter[config.Id] = ++counter;
            }
        }

        UniTask ITimeUpdate.OnUpdateTime(float currentTime, float deltaTime)
        {
            return UniTask.CompletedTask;
        }
        UniTask ITimeUpdate.OnEndUpdateTime()
        {
            m_ExecutionCount.Clear();
            // "Clear execution count config".ToLog();
            return UniTask.CompletedTask;
        }

        void IConnector<IGameConfigProvider>.Connect(IGameConfigProvider    t)
        {
            m_GameConfigProvider = t;
        }

        void IConnector<IGameConfigProvider>.Disconnect(IGameConfigProvider t)
        {
            ConditionTrigger.OnEventExecutedAsync -= Resolve;
            m_GameConfigProvider                  =  null;
        }

        void IConnector<IGameMethodProvider>.Connect(IGameMethodProvider    t) => m_GameMethodProvider = t;
        void IConnector<IGameMethodProvider>.Disconnect(IGameMethodProvider t) => m_GameMethodProvider = null;

        void IConnector<IUserDataProvider>.Connect(IUserDataProvider    t) => m_UserDataProvider = t;
        void IConnector<IUserDataProvider>.Disconnect(IUserDataProvider t) => m_UserDataProvider = null;
    }
}