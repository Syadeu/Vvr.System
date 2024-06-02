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
// File created : 2024, 05, 28 23:05
#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Mainmenu
{
    /// <summary>
    /// Represents a session for the main menu view.
    /// </summary>
    [UsedImplicitly]
    public sealed class MainmenuViewSession : ContentViewChildSession<MainmenuViewEvent>,
        IConnector<IMainmenuViewProvider>,
        IConnector<IActorDataProvider>
    {
        private IAssetProvider        m_AssetProvider;
        private IMainmenuViewProvider m_MainmenuViewProvider;
        private IActorDataProvider    m_ActorDataProvider;

        public override string DisplayName => nameof(MainmenuViewSession);

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            EventHandler
                .Register(MainmenuViewEvent.OpenResearch, OnOpenResearch)
                .Register(MainmenuViewEvent.SetupActorInputs, OnSetupActorInputs)
                .Register(MainmenuViewEvent.Skill1Button, OnSkill1Button)
                .Register(MainmenuViewEvent.Skill2Button, OnSkill2Button)
                ;

            Setup().Forget();
        }

        protected override UniTask OnReserve()
        {
            EventHandler
                .Unregister(MainmenuViewEvent.OpenResearch, OnOpenResearch)
                .Unregister(MainmenuViewEvent.SetupActorInputs, OnSetupActorInputs)
                .Unregister(MainmenuViewEvent.Skill1Button, OnSkill1Button)
                .Unregister(MainmenuViewEvent.Skill2Button, OnSkill2Button)
                ;

            return base.OnReserve();
        }

        private async UniTask OnSetupActorInputs(MainmenuViewEvent e, object ctx)
        {
            IActor actor = (IActor)ctx;

            IActorData data = m_ActorDataProvider.Resolve(actor.Id);

            float
                skill0Cooltime = actor.Skill.GetSkillCooltime(data.Skills[0]),
                skill1Cooltime = actor.Skill.GetSkillCooltime(data.Skills[1])
                ;
            UniTask skill0EnableTask, skill1EnableTask;
            if (skill0Cooltime > 0)
                skill0EnableTask = EventHandler.ExecuteAsync(MainmenuViewEvent.DisableSkillButton, 0);
            else
                skill0EnableTask = EventHandler.ExecuteAsync(MainmenuViewEvent.EnableSkillButton, 0);

            if (skill1Cooltime > 0)
                skill1EnableTask = EventHandler.ExecuteAsync(MainmenuViewEvent.DisableSkillButton, 1);
            else
                skill1EnableTask = EventHandler.ExecuteAsync(MainmenuViewEvent.EnableSkillButton, 1);


            var skillIcons = await UniTask.WhenAll(
                m_AssetProvider.LoadAsync<Sprite>(data.Skills[0].Presentation.Icon),
                m_AssetProvider.LoadAsync<Sprite>(data.Skills[1].Presentation.Icon)
                );

            await UniTask.WhenAll(
                skill0EnableTask,
                skill1EnableTask,

                EventHandler
                    .ExecuteAsync(MainmenuViewEvent.SetSkill1Image, skillIcons.Item1?.Object),
                EventHandler
                    .ExecuteAsync(MainmenuViewEvent.SetSkill2Image, skillIcons.Item2?.Object)
                );
        }

        private async UniTask OnSkill1Button(MainmenuViewEvent e, object ctx)
        {
        }

        private async UniTask OnSkill2Button(MainmenuViewEvent e, object ctx)
        {
        }

        private async UniTask OnOpenResearch(MainmenuViewEvent e, object ctx)
        {
            await EventHandlerProvider
                .Resolve<ResearchViewEvent>()
                .ExecuteAsync(ResearchViewEvent.Open, 0);
        }

        private async UniTaskVoid Setup()
        {
            // Because main menu view should be provided right away
            // after world has been initialized
            await UniTask.WaitWhile(() => m_MainmenuViewProvider == null);

            await m_MainmenuViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, null);
        }

        void IConnector<IMainmenuViewProvider>.Connect(IMainmenuViewProvider t)
        {
            m_MainmenuViewProvider = t;
            m_MainmenuViewProvider.Initialize(EventHandler);
        }

        void IConnector<IMainmenuViewProvider>.Disconnect(IMainmenuViewProvider t)
        {
            m_MainmenuViewProvider.Reserve();
            m_MainmenuViewProvider = null;
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;
    }
}