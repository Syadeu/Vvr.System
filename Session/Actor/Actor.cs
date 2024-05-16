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
// File created : 2024, 05, 14 16:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vvr.Controller;
using Vvr.Controller.Abnormal;
using Vvr.Controller.Actor;
using Vvr.Controller.Asset;
using Vvr.Controller.BehaviorTree;
using Vvr.Controller.Condition;
using Vvr.Controller.Item;
using Vvr.Controller.Passive;
using Vvr.Controller.Skill;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Actor
{
    internal class Actor : ScriptableObject, IActor, IInitialize<Owner, ActorSheet.Row>
    {
        private StatValueStack         m_Stats;
        private ConditionResolver      m_ConditionResolver;
        private AbnormalController     m_AbnormalController;
        private PassiveController      m_PassiveController;
        private SkillController        m_SkillController;
        private ItemInventory          m_ItemInventory;
        private BehaviorTreeController m_BehaviorTreeController;
        private AssetController        m_AssetController;

        public Owner  Owner       { get; private set; }
        public string DisplayName => DataID;
        public bool   Disposed    { get; private set; }

        public bool Initialized { get; private set; }
        public bool Instanced   { get; private set; }

        public string                     DataID            { get; private set; }
        public IReadOnlyConditionResolver ConditionResolver => m_ConditionResolver;
        public IStatValueStack            Stats             => m_Stats;

        public IPassive  Passive  => m_PassiveController;
        public IAbnormal Abnormal => m_AbnormalController;
        public ISkill    Skill    => m_SkillController;

        public IAsset Assets => m_AssetController;

        IActor IActor.CreateInstance()
        {
            var d = Instantiate(this);
            d.Instanced = true;

            return d;
        }
        void IActor.Release()
        {
            Teardown();

            Disposed = true;
            Destroy(this);
        }

        public void Initialize(Owner t, ActorSheet.Row ta)
        {
            Assert.IsFalse(Initialized);

            Owner  = t;
            DataID = ta.Id;

            m_Stats = new StatValueStack(this, ta.Stats);

            m_ItemInventory          = new ItemInventory(this);
            m_ConditionResolver      = new ConditionResolver(this);
            m_AbnormalController     = new AbnormalController(this);
            m_PassiveController      = new PassiveController(this);
            m_SkillController        = new SkillController(this);
            m_BehaviorTreeController = new BehaviorTreeController(this);

            m_AssetController = new AssetController(ta.Assets);

            m_Stats
                .AddModifier(m_AbnormalController)
                .AddModifier(m_ItemInventory);

            m_ConditionResolver
                .Connect(m_AbnormalController)
                .Connect(Stats, StatProvider.Static)

                .Subscribe(m_AbnormalController)
                .Subscribe(m_PassiveController)
                ;

            for (int i = 0; i < ta.Passive?.Count; i++)
            {
                if (ta.Passive[i].Ref == null) continue;

                m_PassiveController.Add(ta.Passive[i].Ref);
            }

            Initialized = true;
        }
        public void Teardown()
        {
            ((IActor)this).DisconnectTime();

            m_ConditionResolver
                .Unsubscribe(m_AbnormalController)
                .Unsubscribe(m_PassiveController);

            m_ConditionResolver?.Dispose();
            m_AbnormalController?.Dispose();
            m_PassiveController?.Dispose();
            m_SkillController?.Dispose();
            m_BehaviorTreeController?.Dispose();
            m_ItemInventory?.Dispose();

            m_ConditionResolver      = null;
            m_AbnormalController     = null;
            m_PassiveController      = null;
            m_SkillController        = null;
            m_BehaviorTreeController = null;
            m_ItemInventory          = null;

            Initialized = false;
        }

        void IActor.ConnectTime()
        {
            TimeController.Register(m_SkillController);
            TimeController.Register(m_AbnormalController);
        }

        void IActor.DisconnectTime()
        {
            TimeController.Unregister(m_SkillController);
            TimeController.Unregister(m_AbnormalController);
        }

        async UniTask IBehaviorTarget.Execute(IReadOnlyList<string> parameters)
        {

        }

        public void Reset()
        {
            m_SkillController.Clear();
            m_AbnormalController.Clear();
        }
    }
}