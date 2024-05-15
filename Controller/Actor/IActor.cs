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
// File created : 2024, 05, 12 18:05

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Abnormal;
using Vvr.Controller.Asset;
using Vvr.Controller.BehaviorTree;
using Vvr.Controller.Condition;
using Vvr.Controller.Passive;
using Vvr.Controller.Skill;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Actor
{
    public interface IActor : IConditionTarget, IBehaviorTarget
    {
        string DataID { get; }

        IStatValueStack Stats    { get; }
        IPassive        Passive  { get; }
        IAbnormal       Abnormal { get; }
        ISkill          Skill    { get; }

        IAsset Assets { get; }

        int GetInstanceID();

        void Initialize(Owner t, ActorSheet.Row ta);

        IActor CreateInstance();

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// If actor has view target(ex. Card) should release first before this method.
        /// </remarks>
        void Release();

        [PublicAPI]
        void ConnectTime();

        [PublicAPI]
        void DisconnectTime();
        [PublicAPI]
        void Reset();
    }
}