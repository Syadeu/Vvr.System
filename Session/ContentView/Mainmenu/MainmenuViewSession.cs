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
using UnityEngine.Assertions;
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
    public sealed class MainmenuViewSession : ContentViewChildSession<MainmenuViewEvent, IMainmenuViewProvider>,
        IConnector<IActorDataProvider>
    {
        private IAssetProvider        m_AssetProvider;
        private IActorDataProvider    m_ActorDataProvider;

        private GameObject m_ViewInstance;

        public override string DisplayName => nameof(MainmenuViewSession);

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);
            Register(m_AssetProvider);

            EventHandler
                .Register(MainmenuViewEvent.OpenResearch, OnOpenResearch)
                .Register(MainmenuViewEvent.OpenActorBatch, OnOpenActorBatch)
                .Register(MainmenuViewEvent.SetupActorInputs, OnSetupActorInputs)
                ;

            Setup()
                .AttachExternalCancellation(ReserveToken)
                .SuppressCancellationThrow()
                .Forget();
        }

        protected override UniTask OnReserve()
        {
            if (m_ViewInstance is not null)
                this.Detach(m_ViewInstance);

            EventHandler
                .Unregister(MainmenuViewEvent.OpenResearch, OnOpenResearch)
                .Unregister(MainmenuViewEvent.OpenActorBatch, OnOpenActorBatch)
                .Unregister(MainmenuViewEvent.SetupActorInputs, OnSetupActorInputs)
                ;

            m_ViewInstance = null;

            return base.OnReserve();
        }

        private async UniTask OnSetupActorInputs(MainmenuViewEvent e, object ctx)
        {
            IActor actor = (IActor)ctx;

            IActorData data = m_ActorDataProvider.Resolve(actor.Id);
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.Skills);
            Assert.IsNotNull(actor.Skill);

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
                m_AssetProvider.LoadAsync<Sprite>(data.Skills[0].IconAssetKey),
                m_AssetProvider.LoadAsync<Sprite>(data.Skills[1].IconAssetKey)
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

        private async UniTask OnOpenResearch(MainmenuViewEvent e, object ctx)
        {
            await EventHandlerProvider
                .Resolve<ResearchViewEvent>()
                .ExecuteAsync(ResearchViewEvent.Open, 0);
        }

        private async UniTask OnOpenActorBatch(MainmenuViewEvent e, object ctx)
        {
            await EventHandlerProvider
                .Resolve<DeckViewEvent>()
                .ExecuteAsync(DeckViewEvent.Open);
        }

        private async UniTask Setup()
        {
            // Because main menu view should be provided right away
            // after world has been initialized
            await UniTask.WaitWhile(() => ViewProvider is null);

            m_ViewInstance = await ViewProvider.OpenAsync(CanvasViewProvider, m_AssetProvider, null);
            this.Inject(m_ViewInstance);
        }

        void IConnector<IActorDataProvider>.Connect(IActorDataProvider t) => m_ActorDataProvider = t;
        void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t) => m_ActorDataProvider = null;
    }
}