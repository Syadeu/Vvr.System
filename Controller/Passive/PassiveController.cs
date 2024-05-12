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
// File created : 2024, 05, 07 18:05

#endregion

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Vvr.System.Model;
using Vvr.System.Provider;

namespace Vvr.System.Controller
{
    public sealed partial class PassiveController : IDisposable
    {
        private static readonly Dictionary<Hash, PassiveController>
            s_CachedController = new();

        public static PassiveController GetOrCreate(IActor o)
        {
#if UNITY_EDITOR
            if (o is UnityEngine.Object uo &&
                uo == null)
            {
                throw new InvalidOperationException();
            }
#endif

            Hash hash = o.GetHash();
            if (!s_CachedController.TryGetValue(hash, out var r))
            {
                r                        = new PassiveController(hash, o);
                s_CachedController[hash] = r;
            }

            return r;
        }

        struct Value : IComparable<Value>
        {
            public RuntimePassive    passive;

            public Value(RuntimePassive data)
            {
                passive = data;
            }

            public int CompareTo(Value other)
            {
                return passive.CompareTo(other.passive);
            }
        }

        private readonly Hash m_Hash;

        private IActor Owner { get; }

        private ITargetProvider m_TargetProvider;

        private readonly List<Value> m_Values = new();

        private PassiveController(Hash hash, IActor o)
        {
            m_Hash              = hash;
            Owner             = o;

            Owner.ConditionResolver.Subscribe(this);
        }

        public void Add(PassiveSheet.Row data)
        {
            RuntimePassive passive = new RuntimePassive(data);

            $"Add passive".ToLog();
            int index = m_Values.BinarySearch(new Value() { passive = passive });

            // no entry
            if (index < 0)
            {
                var v = new Value(passive);
                m_Values.Add(v);
                m_Values.Sort();
                return;
            }

            var boxed = m_Values[index];
            // If newly added passive has higher level
            if (boxed.passive.level < passive.level)
            {
                boxed = new Value(passive);
            }

            m_Values[index] = boxed;
        }

        public void Dispose()
        {
            Owner.ConditionResolver.Unsubscribe(this);
            m_Values.Clear();

            s_CachedController.Remove(m_Hash);

            m_TargetProvider = null;
        }
    }

    partial class PassiveController : IConnector<ITargetProvider>
    {
        void IConnector<ITargetProvider>.Connect(ITargetProvider t)
        {
            Assert.IsNotNull(t);
            Assert.IsNull(m_TargetProvider);
            m_TargetProvider = t;
        }
        void IConnector<ITargetProvider>.Disconnect()
        {
            m_TargetProvider = null;
        }
    }
}