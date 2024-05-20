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
// File created : 2024, 05, 20 23:05

#endregion

using System.Collections.Generic;
using Cathei.BakingSheet.Unity;

namespace Vvr.Model
{
    public interface IDialogueData : IRawData
    {
        IReadOnlyList<IDialogueSpeaker> Speakers { get; }

        IReadOnlyDictionary<AssetType, AddressablePath> Assets { get; }
    }

    public interface IDialogueSpeaker
    {
        IActorData Actor   { get; }
        string     Message { get; }
        /// <summary>
        /// View transform should reference only.
        /// Actual awaiting logics will execute from Controller
        /// </summary>
        float      Time    { get; }
    }
}