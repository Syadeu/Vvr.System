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
// File created : 2024, 05, 06 17:05

#endregion

using System;
using System.Collections.Generic;
using Vvr.Crypto;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    internal readonly struct RuntimeAbnormal : IComparable<RuntimeAbnormal>, IEquatable<RuntimeAbnormal>
    {
        public readonly string id;
        public readonly Hash   hash;
        public readonly int    type, level, maxStack;

        // Definition
        public readonly StatType                targetStat;
        public readonly Method                  methodType;
        public readonly MethodImplDelegate      method;
        public readonly StatValueGetterDelegate getter;
        public readonly StatValueSetterDelegate setter;
        public readonly CryptoFloat             value;

        // Duration
        public readonly CryptoInt   isInfiniteDuration;
        public readonly CryptoFloat delayTime;
        public readonly CryptoFloat duration;

        public readonly IReadOnlyList<Condition> timeCondition;

        // Update
        public readonly bool        enableUpdate;
        public readonly Condition   updateCondition;
        public readonly string      updateValue;
        public readonly CryptoFloat updateInterval;
        public readonly CryptoInt   updateMaxCount;

        // Cancellation
        public readonly Condition   cancelCondition;
        public readonly string      cancelValue;
        public readonly CryptoFloat cancelProbability;
        public readonly bool        cancelClearAllStacks;

        // Chain
        public readonly IReadOnlyList<AbnormalSheet.Reference> abnormalChain;

        public RuntimeAbnormal(AbnormalSheet.Row d)
        {
            id       = d.Id;
            hash     = new Hash(d.Id);
            type     = d.Definition.Type;
            level    = d.Definition.Level;
            maxStack = d.Definition.MaxStack;

            targetStat = d.Definition.TargetStatus.Ref.ToStat();
            methodType = d.Definition.Method;
            method     = d.Definition.Method.ToDelegate();
            getter     = StatValues.GetGetMethod(targetStat);
            setter     = StatValues.GetSetMethod(targetStat);
            value      = d.Definition.Value;

            isInfiniteDuration = d.Duration.Time < 0 ? 1 : 0;
            delayTime          = d.Duration.DelayTime;
            duration           = d.Duration.Time;

            timeCondition = d.TimeCondition;

            enableUpdate    = d.Update.Enable;
            updateCondition = d.Update.Condition;
            updateValue     = d.Update.Value;
            updateInterval  = d.Update.Interval;
            updateMaxCount  = d.Update.MaxCount;

            cancelCondition      = d.Cancellation.Condition;
            cancelValue          = d.Cancellation.Value;
            cancelProbability    = d.Cancellation.Probability;
            cancelClearAllStacks = d.Cancellation.ClearAllStacks;

            abnormalChain = d.AbnormalChain;
        }

        public int CompareTo(RuntimeAbnormal other)
        {
            if (duration < 0 && 0 <= other.duration)
            {
                return 1;
            }
            if (0 <= duration && other.duration < 0)
            {
                return -1;
            }

            if (type < other.type) return -1;
            return type > other.type ? 1 : 0;
        }

        public bool Equals(RuntimeAbnormal other)
        {
            return hash.Equals(other.hash);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeAbnormal other && Equals(other);
        }

        public override int GetHashCode()
        {
            return hash.GetHashCode();
        }
    }
}