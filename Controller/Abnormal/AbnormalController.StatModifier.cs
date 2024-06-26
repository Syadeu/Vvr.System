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
// File created : 2024, 05, 07 03:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Controller.Abnormal
{
    partial class AbnormalController : IStatModifier
    {
        struct ValueMethodOrderComparer : IComparer<Value>
        {
            public static readonly IComparer<Value>   Static   = default(ValueMethodOrderComparer);
            public static readonly Func<Value, Value> Selector = x => x;

            public int Compare(Value x, Value y)
            {
                int xx = (short)x.abnormal.methodType,
                    yy   = (short)y.abnormal.methodType;

                if (xx == 0) xx = 3;
                else xx = xx < (short)Method.AddMultiplier ? 1 : 2;

                if (yy == 0) yy = 3;
                else yy = yy < (short)Method.AddMultiplier ? 1 : 2;

                if (xx < yy) return -1;
                if (xx > yy) return 1;

                if (x.index < y.index) return -1;
                if (x.index > y.index) return 1;
                return 0;
            }
        }

        bool IStatModifier.IsDirty => m_IsDirty;
        int IStatModifier. Order   => StatModifierOrder.Abnormal;

        void IStatModifier.UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalController));

            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1), CancellationToken);

            foreach (Value e in m_Values.OrderBy(ValueMethodOrderComparer.Selector, ValueMethodOrderComparer.Static))
            {
                int length = e.updateCount;
                for (int i = 0; i < length; i++)
                {
                    stats |= e.abnormal.targetStat;
                    e.abnormal.setter(ref stats, e.abnormal.method(
                        e.abnormal.getter(stats),
                        e.abnormal.value
                    ));
                }
            }
            m_IsDirty = false;
        }
    }
}