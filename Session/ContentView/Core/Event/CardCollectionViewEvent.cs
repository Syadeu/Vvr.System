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
// File created : 2024, 06, 04 23:06

#endregion

using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model;

namespace Vvr.Session.ContentView.Core
{
    public enum CardCollectionViewEvent : short
    {
        Open,
        Close,

        ConfirmButton = 9000,

        SelectCard = 10000,
    }

    public struct CardCollectionViewOpenContext
    {
        [CanBeNull]
        public IResolvedActorData selected;
        [NotNull]
        public IReadOnlyList<IResolvedActorData> data;
    }

    public struct CardCollectionViewChangeDeckContext
    {
        public             int                               index;
        [CanBeNull] public IResolvedActorData                selected;
        [NotNull]   public IReadOnlyList<IResolvedActorData> team;
        [NotNull]   public IReadOnlyList<IResolvedActorData> data;
    }
}