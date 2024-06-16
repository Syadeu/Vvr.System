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
// File created : 2024, 05, 17 23:05

#endregion

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Provider;

namespace Vvr.Session.EventView.Core
{
    [PublicAPI]
    public interface IViewRegistryProvider : IProvider
    {
        [Obsolete("Use " + nameof(Resolve))]
        IEventTargetViewProvider    CardViewProvider     { get; }

        [Obsolete("Use " + nameof(Resolve))]
        IEventTimelineNodeViewProvider TimelineNodeViewViewProvider { get; }

        [Obsolete("Use " + nameof(Resolve))]
        IStageViewProvider    StageViewProvider    { get; }

        IReadOnlyDictionary<Type, IEventViewProvider> Providers { get; }

        [NotNull]
        IProvider Resolve(Type providerType);

        [NotNull]
        TProvider Resolve<TProvider>();
    }
}