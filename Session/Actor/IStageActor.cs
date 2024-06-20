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

using System;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Actor
{
    [PublicAPI]
    public interface IStageActor : IDisposable,
        IConnector<IAssetProvider>,
        IConnector<IActorViewProvider>,
        IConnector<ITargetProvider>,
        IConnector<IActorDataProvider>,
        IConnector<IEventConditionProvider>,
        IConnector<IStateConditionProvider>
    {
        IActor     Owner           { get; }
        IActorData Data            { get; }

        // ActorState State { get; set; }

        bool TagOutRequested { get; set; }
        bool OverrideFront   { get; set; }

        bool Disposed { get; }
    }

    // [Flags]
    // [PublicAPI]
    // public enum ActorState
    // {
    //     None = 0,
    //
    //     CanTag   = 0b0001,
    //     CanParry = 0b0010
    // }
}