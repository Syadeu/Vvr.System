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
// File created : 2024, 05, 06 19:05

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Stat;
using Vvr.Crypto;
using Vvr.Model;
using Vvr.UI.Observer;

namespace Vvr.Controller.Abnormal
{
    public sealed partial class AbnormalController : IAbnormal, IDisposable
    {
        struct Value : IComparable<Value>, IReadOnlyRuntimeAbnormal
        {
            public UniqueID uniqueID;

            public CryptoFloat     delayDuration, duration;
            public CryptoInt       stack;
            public RuntimeAbnormal abnormal;

            public CryptoFloat lastUpdatedTime;
            public CryptoInt   updateCount;

            public uint index;

            public int CompareTo(Value other)
            {
                return abnormal.CompareTo(other.abnormal);
            }

            string IReadOnlyRuntimeAbnormal.Id      => abnormal.id;
            bool IReadOnlyRuntimeAbnormal.  Enabled => updateCount > 0;
            int IReadOnlyRuntimeAbnormal.   Stack   => stack;
        }

        private readonly List<Value>             m_Values                  = new();
        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        private readonly SemaphoreSlim m_Lock = new(1, 1);

        private uint m_Counter;
        private bool m_IsDirty;

        public  bool              Disposed          { get; private set; }
        private CancellationToken CancellationToken => m_CancellationTokenSource.Token;

        public IActor Owner { get; }
        public int    Count => m_Values.Count;

        public bool IsDirty => m_IsDirty;

        public AbnormalController(IActor owner)
        {
            Owner  = owner;

            m_IsDirty = true;
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalController));

            m_CancellationTokenSource.Cancel();

            TimeController.Unregister(this);

            Disposed = true;
        }

        public void Clear()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalController));

            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1), CancellationToken);

            m_Values.Clear();
            m_Counter = 0;
        }

        public async UniTask<AbnormalHandle> AddAsync(IAbnormalData data)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalController));

            var abnormal = new RuntimeAbnormal(data);
            for (int i = 0; i < abnormal.abnormalChain?.Count; i++)
            {
                await AddAsync(abnormal.abnormalChain[i])
                    .AttachExternalCancellation(CancellationToken)
                    ;

                if (CancellationToken.IsCancellationRequested)
                    return default;
            }

            if (CancellationToken.IsCancellationRequested)
                return default;

            Value value = default;
            using (var wl = new SemaphoreSlimLock(m_Lock))
            {
                await wl.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken);
                if (!AddInternal(ref abnormal, ref value))
                {
                    return default;
                }
            }

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Abnormal);
            await trigger.Execute(Model.Condition.OnAbnormalAdded, data.Id, CancellationToken);

            return new AbnormalHandle(this, abnormal.id, value.uniqueID);
        }

        public bool IsActivated(in AbnormalHandle handle)
        {
            int index = IndexOf(in handle);
            if (index < 0) return false;

            return m_Values[index].updateCount > 0;
        }

        public float GetDuration(in AbnormalHandle handle)
        {
            int index = IndexOf(in handle);
            if (index < 0) throw new InvalidOperationException();

            return m_Values[index].delayDuration + m_Values[index].duration;
        }

        private int IndexOf(in AbnormalHandle handle)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1), CancellationToken);

            for (int i = 0; i < m_Values.Count; i++)
            {
                var e = m_Values[i];
                if (e.uniqueID != handle.UniqueID) continue;

                return i;
            }

            return -1;
        }
        private bool AddInternal(ref RuntimeAbnormal abnormal, ref Value value)
        {
            if (abnormal.maxStack < 0)
            {
                $"Trying to add abnormal has max stack {abnormal.maxStack}".ToLogError();
                return false;
            }

            // $"Add abnormal {abnormal.hash}".ToLog();

            // Prevent overflow
            if (uint.MaxValue - 1000 < m_Counter + 1)
            {
                uint start = m_Counter - (uint)m_Values.Count;
                for (int i = 0; i < m_Values.Count; i++)
                {
                    var temp = m_Values[i];
                    temp.index  -= start;
                    m_Values[i] =  temp;
                }

                m_Counter = 0;
            }

            int index = m_Values.BinarySearch(new Value() { abnormal = abnormal });

            // If no entry
            if (index < 0)
            {
                value = new Value()
                {
                    uniqueID = UniqueID.Issue,

                    abnormal      = abnormal,
                    duration      = abnormal.duration,
                    delayDuration = abnormal.delayTime > 0 ? abnormal.delayTime : 0,
                    updateCount   = abnormal.delayTime > 0 ? 0 : 1,
                    stack         = 1,

                    index = m_Counter++,
                };
                m_Values.Add(value);
                // Full list sort is required since runtime abnormal order will be changed by duration
                m_Values.Sort();
                m_IsDirty = true;

                return true;
            }

            value = m_Values[index];
            // If newly added abnormal has higher level
            if (value.abnormal.level < abnormal.level)
            {
                value = new Value()
                {
                    uniqueID = UniqueID.Issue,

                    abnormal      = abnormal,
                    duration      = abnormal.duration,
                    delayDuration = abnormal.delayTime > 0 ? abnormal.delayTime : 0,
                    updateCount   = abnormal.delayTime > 0 ? 0 : 1,
                    stack         = 1,

                    index = m_Counter++,
                };
            }
            else
            {

                if ( // if max stack is lower than 0, take as infinite stack
                    abnormal.maxStack > 0 &&
                    // If exceed max stack
                    abnormal.maxStack <= m_Values[index].stack)
                {
                    // Update duration for current time
                    value.duration = abnormal.duration;
                }
                // add stack
                else value.stack++;
            }

            m_Values[index] = value;
            m_IsDirty       = true;

            return true;
        }

        public bool Contains(in AbnormalHandle handle)
        {
            int index = IndexOf(in handle);
            // $"{index} {index >= 0}".ToLog();
            return index >= 0;
        }
        public async UniTask RemoveAsync(AbnormalHandle handle)
        {
            bool removed = false;
            using (var wl = new SemaphoreSlimLock(m_Lock))
            {
                await wl.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken);
                for (int i = 0; i < m_Values.Count; i++)
                {
                    var e = m_Values[i];
                    if (e.uniqueID != handle.UniqueID) continue;

                    m_Values.RemoveAt(i);
                    removed = true;
                    break;
                }
            }

            if (!removed) return;

            m_IsDirty = true;

            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Abnormal);
            await trigger.Execute(Model.Condition.OnAbnormalRemoved, handle.Id, CancellationToken);
        }
        public bool Contains(Hash abnormalId)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalController));

            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1), CancellationToken);

            return m_Values.Any(x => x.abnormal.hash == abnormalId);
        }

        public IEnumerator<IReadOnlyRuntimeAbnormal> GetEnumerator()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalController));

            Value[] result;
            using (var wl = new SemaphoreSlimLock(m_Lock))
            {
                wl.Wait(TimeSpan.FromSeconds(1), CancellationToken);
                result = m_Values.ToArray();
            }

            return result.Select(x => (IReadOnlyRuntimeAbnormal)x).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region ConditionObserver

        private partial bool CheckCancellation(ref int index, EventCondition condition);

        #endregion

        #region TimeUpdate

        private partial bool CheckTimeCondition(Value value);

        #endregion
    }
}