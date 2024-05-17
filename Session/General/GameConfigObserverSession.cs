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
    public sealed class GameConfigObserverSession : ChildSession<GameConfigObserverSession.SessionData>,
        ITimeUpdate,
        IConnector<IGameConfigProvider>
    {
        public struct SessionData : ISessionData
        {
            public readonly MapType configType;

            public SessionData(MapType t)
            {
                configType = t;
            }
        }

        private IGameConfigProvider              m_GameConfigProvider;
        private IEnumerable<GameConfigSheet.Row> m_Configs;

        private readonly Dictionary<Hash, int> m_ExecutionCount = new();

        public override string DisplayName => nameof(GameConfigObserverSession);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            m_Configs = m_GameConfigProvider[data.configType];

            ConditionTrigger.OnEventExecutedAsync += OnEventExecutedAsync;

            TimeController.Register(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            ConditionTrigger.OnEventExecutedAsync -= OnEventExecutedAsync;

            TimeController.Unregister(this);

            return base.OnReserve();
        }

        private async UniTask OnEventExecutedAsync(IEventTarget e, Condition condition, string value)
        {
            Assert.IsFalse(e.Disposed);

            // TODO: temp
            if (e is not IActor target) return;

            foreach (var config in m_Configs)
            {
                // Prevent infinite loop
                await UniTask.Yield();

                if (!EvaluateActorConfig(config, target)) continue;
                if (!EvaluateExecutionCount(config, target, out int executedCount)) continue;

                $"[World] execute config : {target.DisplayName} : {condition}, {value}".ToLog();

                m_ExecutionCount[target.GetHash()] = ++executedCount;
                await ExecuteMethod(target, config.Execution.Method, config.Parameters);
            }
        }

        private bool EvaluateActorConfig(GameConfigSheet.Row config, IActor target)
        {
            Assert.IsNotNull(config);

            // Check lifecycle condition
            if (config.Lifecycle.Condition != 0)
            {
                if (!target.ConditionResolver[(Condition)config.Lifecycle.Condition](config.Lifecycle.Value))
                {
                    return false;
                }
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

            return true;
        }

        private bool EvaluateExecutionCount(GameConfigSheet.Row config, IActor target, out int executedCount)
        {
            Hash hash = target.GetHash();
            m_ExecutionCount.TryGetValue(hash, out executedCount);

            if (0 > config.Evaluation.MaxCount) return true;

            // Exceed max execution count
            return config.Evaluation.MaxCount > executedCount;
        }

        private async UniTask ExecuteMethod(IEventTarget o, Model.GameMethod method, IReadOnlyList<string> parameters)
        {
            var methodProvider = GetProviderRecursive<IGameMethodProvider>();
            Assert.IsNotNull(methodProvider, "methodProvider != null");
            await methodProvider.Resolve(method)(o, parameters);
        }

        UniTask ITimeUpdate.OnUpdateTime(int currentTime, int deltaTime)
        {
            return UniTask.CompletedTask;
        }
        UniTask ITimeUpdate.OnEndUpdateTime()
        {
            m_ExecutionCount.Clear();
            return UniTask.CompletedTask;
        }

        void IConnector<IGameConfigProvider>.    Connect(IGameConfigProvider        t) => m_GameConfigProvider = t;
        void IConnector<IGameConfigProvider>.    Disconnect(IGameConfigProvider     t) => m_GameConfigProvider = null;
    }
}