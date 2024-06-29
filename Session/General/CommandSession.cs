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
// File created : 2024, 05, 30 00:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Provider.Command;
using Vvr.Session;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class CommandSession : ChildSession<CommandSession.SessionData>,
        ICommandProvider
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(CommandSession);

        private readonly Dictionary<Type, DependencyInfo> m_DependencyInfo = new();

        protected override UniTask OnReserve()
        {
            m_DependencyInfo.Clear();

            return base.OnReserve();
        }

        public async UniTask EnqueueAsync<TCommand>(IEventTarget target) where TCommand : ICommand
        {
            await EnqueueAsync(target, Activator.CreateInstance<TCommand>());
        }
        public async UniTask EnqueueAsync(IEventTarget target, ICommand command)
        {
            var t = command.GetType();
            if (!m_DependencyInfo.TryGetValue(t, out var info))
            {
                info                = command.GetDependencyInfo(t);
                m_DependencyInfo[t] = info;
            }

            this.Inject(command, info);

            // TODO: maybe record?

            $"[{target.DisplayName}] Execute command {command.GetType().Name}".ToLog();
            await command.ExecuteAsync(target);

            this.Detach(command, info);
        }
    }
}
