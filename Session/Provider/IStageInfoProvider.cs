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
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;

namespace Vvr.Session.Provider
{
    [LocalProvider]
    public interface IStageInfoProvider : IProvider
    {
        IReadOnlyList<IStageActor> Timeline     { get; }

        IReadOnlyActorList HandActors  { get; }
        IReadOnlyActorList PlayerField { get; }
        IReadOnlyActorList EnemyField  { get; }

        UniTask Delete(IActor actor);
    }
}