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
// File created : 2024, 05, 16 23:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Provider;
using Vvr.Provider.Command;
using Vvr.Session.Actor;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.Input
{
    [UsedImplicitly]
    public sealed class PlayerControlSession : AIControlSession,
        IConnector<IContentViewEventHandlerProvider>,
        IConnector<ICommandProvider>
    {
        private IContentViewEventHandlerProvider m_ContentViewEventHandlerProvider;

        private UniTaskCompletionSource m_ActionCompletionSource;
        private IActor                  m_CurrentControl;

        private ICommandProvider m_CommandProvider;

        public override string DisplayName => nameof(PlayerControlSession);

        public override bool CanControl(IEventTarget target)
        {
            return target is IActor;
        }

        protected override async UniTask OnControl(IEventTarget  target)
        {
            // TODO: testing
            if (Owner != target.Owner || target is not IActor actor)
            {
                // AI
                await base.OnControl(target);
                return;
            }

            m_CurrentControl = actor;

            m_ActionCompletionSource = new();
            RegisterInput(actor);

            await m_ActionCompletionSource.Task;

            UnregisterInput();
            m_CurrentControl = null;
        }

        private bool m_InputRegistered;

        private async UniTask RegisterInput(IActor actor)
        {
            if (m_InputRegistered) return;

            m_ContentViewEventHandlerProvider.Resolve<MainmenuViewEvent>()
                .Register(MainmenuViewEvent.Skill1Button, OnSkill1Button)
                .Register(MainmenuViewEvent.Skill2Button, OnSkill2Button)
                ;

            await m_ContentViewEventHandlerProvider.Resolve<MainmenuViewEvent>()
                .ExecuteAsync(MainmenuViewEvent.SetupActorInputs, actor);

            m_ContentViewEventHandlerProvider.Resolve<MainmenuViewEvent>()
                .ExecuteAsync(MainmenuViewEvent.ShowActorInputs, actor)
                .Forget();

            m_InputRegistered = true;
        }
        private void UnregisterInput()
        {
            if (!m_InputRegistered) return;

            m_ContentViewEventHandlerProvider.Resolve<MainmenuViewEvent>()
                .Unregister(MainmenuViewEvent.Skill1Button, OnSkill1Button)
                .Unregister(MainmenuViewEvent.Skill2Button, OnSkill2Button)
                ;
            m_ContentViewEventHandlerProvider.Resolve<MainmenuViewEvent>()
                .ExecuteAsync(MainmenuViewEvent.HideActorInputs)
                .Forget();

            m_InputRegistered = false;
        }

        private async UniTask OnSkill2Button(MainmenuViewEvent e, object ctx)
        {
            if (m_CurrentControl == null)
                throw new InvalidOperationException();

            UnregisterInput();
            await m_CommandProvider.EnqueueAsync(m_CurrentControl, new SkillExecuteCommand(1));

            if (!m_ActionCompletionSource.TrySetResult())
                throw new InvalidOperationException("Already executed");
        }
        private async UniTask OnSkill1Button(MainmenuViewEvent e, object ctx)
        {
            if (m_CurrentControl == null)
                throw new InvalidOperationException();

            UnregisterInput();
            await m_CommandProvider.EnqueueAsync(m_CurrentControl, new SkillExecuteCommand(0));

            if (!m_ActionCompletionSource.TrySetResult())
                throw new InvalidOperationException("Already executed");
        }

        void IConnector<IContentViewEventHandlerProvider>.Connect(IContentViewEventHandlerProvider    t) => m_ContentViewEventHandlerProvider = t;
        void IConnector<IContentViewEventHandlerProvider>.Disconnect(IContentViewEventHandlerProvider t) => m_ContentViewEventHandlerProvider = null;

        void IConnector<ICommandProvider>.Connect(ICommandProvider    t) => m_CommandProvider = t;
        void IConnector<ICommandProvider>.Disconnect(ICommandProvider t) => m_CommandProvider = null;
    }
}