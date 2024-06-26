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
// File created : 2024, 06, 17 21:06

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller;
using Vvr.Controller.Abnormal;
using Vvr.Controller.Actor;
using Vvr.Controller.Asset;
using Vvr.Controller.BehaviorTree;
using Vvr.Controller.Condition;
using Vvr.Controller.Passive;
using Vvr.Controller.Skill;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.TestClass
{
    [PublicAPI]
    public class TestActor : TestConditionTarget, IActor
    {
        public static TestActor Create(Owner owner, string displayName = null)
        {
            string id = unchecked((int)FNV1a32.Calculate(Guid.NewGuid())).ToString();
            if (displayName.IsNullOrEmpty())
            {
                displayName = id;
            }

            TestActor t = new TestActor(owner, displayName, id);
            return t;
        }

        private readonly int m_InstanceId = unchecked((int)FNV1a32.Calculate(Guid.NewGuid()));

        private StatValueStack     m_Stats;
        private AbnormalController m_AbnormalController;
        private PassiveController  m_PassiveController;
        private SkillController    m_SkillController;

        public string          Id       { get; set; }
        public IStatValueStack Stats    { get; set; }
        public IPassive        Passive  { get; set; }
        public IAbnormal       Abnormal { get; set; }
        public ISkill          Skill    { get; set; }
        public IAsset          Assets   { get; set; }

        public TestActor(Owner owner, string displayName, string id)
            : base(owner, displayName)
        {
            Id = id;
        }

        async UniTask IBehaviorTarget.Execute(IReadOnlyList<string> parameters)
        {
            throw new NotImplementedException();
        }


        public IActor          CreateInstance()
        {
            throw new NotImplementedException();
        }

        public int GetInstanceID() => m_InstanceId;

        public void Initialize(Owner owner, IStatConditionProvider statConditionProvider, IActorData ta)
        {
            ConditionResolver = new ConditionResolver(this);

            m_Stats = new StatValueStack(this, ta.Stats);
            m_Stats.AddPostProcessor(HpShieldPostProcessor.Static);

            m_AbnormalController = new AbnormalController(this);
            m_PassiveController  = new PassiveController(this);
            m_SkillController    = new SkillController(this);

            m_Stats
                .AddModifier(m_AbnormalController)
                ;

            ConditionResolver
                .Connect(m_AbnormalController)
                .Connect(Stats, statConditionProvider)

                .Subscribe(m_AbnormalController)
                .Subscribe(m_PassiveController)
                ;

            for (int i = 0; i < ta.Passive?.Count; i++)
            {
                if (ta.Passive[i] == null) continue;

                m_PassiveController.Add(ta.Passive[i]);
            }
        }

        public void Release()
        {
            ((IActor)this).DisconnectTime();

            ConditionResolver
                .Unsubscribe(m_AbnormalController)
                .Unsubscribe(m_PassiveController);

            ConditionResolver?.Dispose();
            m_AbnormalController?.Dispose();
            m_PassiveController?.Dispose();
            m_SkillController?.Dispose();

            ConditionResolver    = null;
            m_AbnormalController = null;
            m_PassiveController  = null;
            m_SkillController    = null;
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

        public void Reset()
        {
            m_SkillController.Clear();
            m_AbnormalController.Clear();
        }
    }
}