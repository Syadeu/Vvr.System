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
// File created : 2024, 05, 16 22:05

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
    internal sealed class StageActor : IStageActor, IActor
    {
        public readonly IActor         owner;
        public readonly ActorSheet.Row data;

        public bool tagOutRequested;

        public StageActor(IActor o, ActorSheet.Row d)
        {
            owner = o;
            data  = d;

            tagOutRequested = false;
        }

        IActor IStageActor.        Owner       => owner;
        ActorSheet.Row IActorData.Data        => data;

        Owner IEventTarget. Owner       => owner.Owner;
        string IEventTarget.DisplayName => owner.DisplayName;
        bool IEventTarget.  Disposed    => owner.Disposed;

        async UniTask IBehaviorTarget.Execute(IReadOnlyList<string> parameters) => await owner.Execute(parameters);

        IReadOnlyConditionResolver IConditionTarget.ConditionResolver => owner.ConditionResolver;

        string IActor.         DataID   => owner.DataID;
        IStatValueStack IActor.Stats    => owner.Stats;
        IPassive IActor.       Passive  => owner.Passive;
        IAbnormal IActor.      Abnormal => owner.Abnormal;
        ISkill IActor.         Skill    => owner.Skill;
        IAsset IActor.         Assets   => owner.Assets;

        int IActor.GetInstanceID() => owner.GetInstanceID();
        void IActor.  Initialize(Owner t, ActorSheet.Row ta) => owner.Initialize(t, ta);
        IActor IActor.CreateInstance() => owner.CreateInstance();

        void IActor.Release() => owner.Release();
        void IActor.ConnectTime() => owner.ConnectTime();
        void IActor.DisconnectTime() => owner.DisconnectTime();
        void IActor.Reset() => owner.Reset();
    }
}