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
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Stat;
using Vvr.Crypto;
using Vvr.Model;
using Vvr.MPC.Provider;
using Vvr.UI.Observer;

namespace Vvr.Controller.Abnormal
{
    public sealed partial class AbnormalController : IAbnormal, IDisposable
    {
        private static readonly Dictionary<Hash, AbnormalController>
            s_CachedControllers = new();

        public static AbnormalController GetOrCreate(IActor o)
        {
            Hash hash = o.GetHash();

            if (!s_CachedControllers.TryGetValue(hash, out var r))
            {
                r = new(o, hash);
                TimeController.Register(r);
                s_CachedControllers.Add(hash, r);
            }

            return r;
        }

        struct Value : IComparable<Value>, IReadOnlyRuntimeAbnormal
        {
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

        private readonly Hash         m_Hash;
        private readonly List<Value>  m_Values = new();

        private uint m_Counter;

#if UNITY_EDITOR
        private bool Disposed { get; set; }
#endif

        public IActor Owner { get; }
        public int    Count => m_Values.Count;

        private AbnormalController(IActor owner, Hash hash)
        {
            Owner  = owner;
            m_Hash = hash;

            Owner.ConditionResolver.Subscribe(this);

            m_IsDirty = true;
            ObjectObserver<AbnormalController>.Get(this).EnsureContainer();
        }

        public async UniTask Add(AbnormalSheet.Row data)
        {
            var abnormal = new RuntimeAbnormal(data);
            for (int i = 0; i < abnormal.abnormalChain?.Count; i++)
            {
                await Add(abnormal.abnormalChain[i].Ref);
            }
            if (abnormal.maxStack < 0)
            {
                $"Trying to add abnormal has max stack {abnormal.maxStack}".ToLogError();
                return;
            }

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

            $"Add abnormal {abnormal.hash}".ToLog();
            using var trigger = ConditionTrigger.Push(Owner, ConditionTrigger.Abnormal);
            int       index   = m_Values.BinarySearch(new Value() { abnormal = abnormal });

            // If no entry
            if (index < 0)
            {
                var v = new Value()
                {
                    abnormal      = abnormal,
                    duration      = abnormal.duration,
                    delayDuration = abnormal.delayTime > 0 ? abnormal.delayTime : 0,
                    updateCount   = abnormal.delayTime > 0 ? 0 : 1,
                    stack = 1,

                    index = m_Counter++
                };
                m_Values.Add(v);
                m_Values.Sort();
                m_IsDirty = true;
                ObjectObserver<AbnormalController>.ChangedEvent(this);
                ObjectObserver<IStatValueStack>.ChangedEvent(Owner.Stats);
                await trigger.Execute(Model.Condition.OnAbnormalAdded, data.Id);
                return;
            }

            var boxed = m_Values[index];
            // If newly added abnormal has higher level
            if (boxed.abnormal.level < abnormal.level)
            {
                boxed = new Value()
                {
                    abnormal    = abnormal,
                    duration      = abnormal.duration,
                    delayDuration = abnormal.delayTime > 0 ? abnormal.delayTime : 0,
                    updateCount   = abnormal.delayTime > 0 ? 0 : 1,
                    stack = 1,

                    index = m_Counter++
                };
            }
            else
            {

                if (// if maxstack is lower than 0, take as infinite stack
                    abnormal.maxStack > 0 &&
                    // If exceed maxstack
                    abnormal.maxStack <= m_Values[index].stack)
                {
                    // Update duration for current time
                    boxed.duration = abnormal.duration;
                }
                // add stack
                else boxed.stack++;
            }

            m_Values[index] = boxed;
            m_IsDirty       = true;
            ObjectObserver<AbnormalController>.ChangedEvent(this);
            ObjectObserver<IStatValueStack>.ChangedEvent(Owner.Stats);
            await trigger.Execute(Model.Condition.OnAbnormalAdded, data.Id);
        }

        public bool Contains(Hash abnormalId)
        {
            return m_Values.Any(x => x.abnormal.hash == abnormalId);
        }


        public void Dispose()
        {
            ObjectObserver<AbnormalController>.Remove(this);
            TimeController.Unregister(this);

            Owner.ConditionResolver.Unsubscribe(this);

#if UNITY_EDITOR
            Disposed = true;
            DOVirtual.DelayedCall(1, () => s_CachedControllers.Remove(m_Hash));
            return;
#endif
            s_CachedControllers.Remove(m_Hash);
        }

        public IEnumerator<IReadOnlyRuntimeAbnormal> GetEnumerator()
        {
            return m_Values.Select(x => (IReadOnlyRuntimeAbnormal)x).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}