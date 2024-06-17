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
using Vvr.Controller.Abnormal;
using Vvr.Controller.Actor;
using Vvr.Controller.Asset;
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
        private readonly int m_InstanceId = unchecked((int)FNV1a32.Calculate(Guid.NewGuid()));

        public string          Id       { get; set; }
        public IStatValueStack Stats    { get; set; }
        public IPassive        Passive  { get; set; }
        public IAbnormal       Abnormal { get; set; }
        public ISkill          Skill    { get; set; }
        public IAsset          Assets   { get; set; }

        public TestActor(Owner owner, string displayName, IStatValueStack stats)
            : base(owner, displayName)
        {
            Stats = stats;
        }

        public async UniTask         Execute(IReadOnlyList<string> parameters)
        {
            throw new NotImplementedException();
        }


        public IActor          CreateInstance()
        {
            throw new NotImplementedException();
        }

        public int GetInstanceID() => m_InstanceId;

        public void            Initialize(Owner owner, IStatConditionProvider statConditionProvider, IActorData ta)
        {
            throw new NotImplementedException();
        }

        public void            Release()
        {
            throw new NotImplementedException();
        }

        public void            ConnectTime()
        {
            throw new NotImplementedException();
        }

        public void            DisconnectTime()
        {
            throw new NotImplementedException();
        }

        public void            Reset()
        {
            throw new NotImplementedException();
        }
    }
}