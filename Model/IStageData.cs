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
// File created : 2024, 05, 17 16:05

#endregion

using System.Collections.Generic;
using Cathei.BakingSheet.Unity;

namespace Vvr.Model
{
    public interface IStageData : IRawData
    {
        string Name { get; }

        int Region { get; }
        int Floor  { get; }

        bool IsLastStage    { get; }
        bool IsLastOfRegion { get; }

        IReadOnlyList<IActorData>                       Actors { get; }
        IReadOnlyDictionary<AssetType, AddressablePath> Assets { get; }
    }
}