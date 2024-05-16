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
// File created : 2024, 05, 10 20:05

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

namespace Vvr.Session.Actor
{
    public interface IStageActor : IActor, IActorData
    {
        IActor Owner           { get; }
        bool   TagOutRequested { get; set; }
    }

    public struct CachedActor : IActor, IActorData
    {
        public IActor       Owner       { get; }
        string IEventTarget.DisplayName => Owner.DisplayName;

        bool IEventTarget.    Disposed => Owner.Disposed;

        async UniTask IBehaviorTarget.Execute(IReadOnlyList<string> parameters)
        {
            await Owner.Execute(parameters);
        }

        public ActorSheet.Row Data        { get; }

        internal CachedActor(IStageActor d)
        {
            Owner = d.Owner;
            Data  = d.Data;
        }

        Owner IEventTarget.                         Owner => Owner.Owner;

        IReadOnlyConditionResolver IConditionTarget.ConditionResolver => Owner.ConditionResolver;

        string IActor.                              DataID => Owner.DataID;

        IStatValueStack IActor.                     Stats => Owner.Stats;

        IPassive IActor.                            Passive => Owner.Passive;

        IAbnormal IActor.                           Abnormal => Owner.Abnormal;

        ISkill IActor.                              Skill => Owner.Skill;

        IAsset IActor.                              Assets => Owner.Assets;

        int IActor.GetInstanceID()
        {
            return Owner.GetInstanceID();
        }

        void IActor.Initialize(Owner t, ActorSheet.Row ta)
        {
            Owner.Initialize(t, ta);
        }

        IActor IActor.CreateInstance()
        {
            return Owner.CreateInstance();
        }

        void IActor.Release()
        {
            Owner.Release();
        }

        void IActor.ConnectTime()
        {
            Owner.ConnectTime();
        }

        void IActor.DisconnectTime()
        {
            Owner.DisconnectTime();
        }

        void IActor.Reset()
        {
            Owner.Reset();
        }
    }
}