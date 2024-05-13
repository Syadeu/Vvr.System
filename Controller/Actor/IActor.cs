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

using Vvr.MPC.Provider;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    public interface IActor : IConditionTarget
    {
        IStatValueStack Stats    { get; }
        IAbnormal       Abnormal { get; }
        ISkill          Skill    { get; }

        int GetInstanceID();

        void Initialize(Owner t, ActorSheet.Row ta);

        IActor CreateInstance();
        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// If actor has view target(ex. Card) should release first before this method.
        /// </remarks>
        void   Release();
    }
}