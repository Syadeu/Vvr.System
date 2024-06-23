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
// File created : 2024, 06, 23 01:06

#endregion

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vvr.Model
{
    [PublicAPI]
    public interface IAbnormalData : IRawData
    {
        IAbnormalDefinition      Definition    { get; }
        IAbnormalDuration        Duration      { get; }
        IReadOnlyList<Condition> TimeCondition { get; }
        IAbnormalUpdate          Update        { get; }
        IAbnormalCancellation    Cancellation  { get; }

        IReadOnlyList<IAbnormalData> AbnormalChain { get; }
    }

    [PublicAPI]
    public interface IAbnormalDefinition
    {
        int       Type         { get; }
        int       Level        { get; }
        bool      IsBuff       { get; }
        bool      Replaceable  { get; }
        int       MaxStack     { get; }
        Method    Method       { get; }
        IStatData TargetStatus { get; }
        float     Value        { get; }
    }

    [PublicAPI]
    public interface IAbnormalDuration
    {
        float DelayTime { get; }
        float Time      { get; }
    }

    [PublicAPI]
    public interface IAbnormalUpdate
    {
        bool      Enable    { get; }
        Condition Condition { get; }
        string    Value     { get; }
        float     Interval  { get; }
        int       MaxCount  { get; }
    }

    [PublicAPI]
    public interface IAbnormalCancellation
    {
        Condition Condition      { get;  }
        float     Probability    { get;  }
        string    Value          { get;  }
        bool      ClearAllStacks { get;  }
    }
}