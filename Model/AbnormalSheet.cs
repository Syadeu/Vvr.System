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
// File created : 2024, 05, 05 12:05

#endregion

using System.Collections.Generic;
using System.Linq;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Buffer;

namespace Vvr.System.Model
{
    [Preserve]
    public sealed class AbnormalSheet : Sheet<AbnormalSheet.Row>
    {
        public struct Definition
        {
            [UsedImplicitly] public int                   Type         { get; private set; }
            [UsedImplicitly] public int                   Level        { get; private set; }
            [UsedImplicitly] public bool                  IsBuff       { get; private set; }
            [UsedImplicitly] public bool                  Replaceable  { get; private set; }
            [UsedImplicitly] public int                   MaxStack     { get; private set; }
            [UsedImplicitly] public Method                Method       { get; private set; }
            [UsedImplicitly] public StatSheet.Reference TargetStatus { get; private set; }
            [UsedImplicitly] public float                 Value        { get; private set; }
        }

        public struct Duration
        {
            [UsedImplicitly] public float DelayTime { get; private set; }
            [UsedImplicitly] public float Time      { get; private set; }
        }

        public struct Update
        {
            [UsedImplicitly] public bool      Enable    { get; private set; }
            [UsedImplicitly] public Condition Condition { get; private set; }
            [UsedImplicitly] public string    Value     { get; private set; }
            [UsedImplicitly] public float     Interval  { get; private set; }
            [UsedImplicitly] public int       MaxCount  { get; private set; }
        }

        public struct Cancellation
        {
            [UsedImplicitly] public Condition Condition      { get; private set; }
            [UsedImplicitly] public float     Probability    { get; private set; }
            [UsedImplicitly] public string    Value          { get; private set; }
            [UsedImplicitly] public bool      ClearAllStacks { get; private set; }
        }

        public sealed class Row : SheetRow
        {
            [UsedImplicitly] public Definition      Definition    { get; private set; }
            [UsedImplicitly] public Duration        Duration      { get; private set; }
            [UsedImplicitly] public List<Condition> TimeCondition { get; private set; }
            [UsedImplicitly] public Update          Update        { get; private set; }
            [UsedImplicitly] public Cancellation    Cancellation  { get; private set; }

            [UsedImplicitly] public List<AbnormalSheet.Reference> AbnormalChain { get; private set; }
        }

        public AbnormalSheet()
        {
            Name = "Abnormal";
        }
    }
}