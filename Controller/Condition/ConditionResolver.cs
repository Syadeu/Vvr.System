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
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.Assertions;
using Vvr.MPC.Provider;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    public delegate bool ConditionDelegate(string value);

    public sealed class ConditionResolver : IReadOnlyConditionResolver, IDisposable,
        IConnector<IEventConditionProvider>,
        IConnector<IStateConditionProvider>
    {
        public static readonly ConditionDelegate Always = _ => true;
        public static readonly ConditionDelegate False = _ => false;

        public static ConditionResolver Create(IEventTarget o)
        {
            Hash hash = o.GetHash();
            return new(o, hash);
        }
        internal static async UniTask Execute(IEventTarget o, Condition condition, string v)
        {
            var resolver = Create(o);

            for (int i = 0; i < resolver.m_EventObservers.Count; i++)
            {
                var e = resolver.m_EventObservers[i];
                if ((e.Filter & condition) != condition) continue;

                await e.OnExecute(condition);
            }
        }

        private readonly IEventTarget m_Owner;
        private readonly Hash         m_Hash;

        private readonly ConditionDelegate[]
            m_Delegates = new ConditionDelegate[VvrTypeHelper.Enum<Condition>.Length];

        private readonly List<IConditionObserver> m_EventObservers = new();

        public ConditionDelegate this[Condition t]
        {
            get
            {
                if (t == Condition.Always) return Always;

                if (m_Delegates[(int)t] == null)
                {
                    $"[Condition] Condition {t} is not connected.".ToLog();
                    return False;
                }

                return m_Delegates[(int)t];
            }
            set
            {
#if UNITY_EDITOR
                var target = m_Delegates[(int)t];
                if (target != null && target.GetInvocationList().Length > 0)
                {
                    StringBuilder sb = new();
                    sb.AppendLine("Chaining condition will leads unexpected result.");
                    sb.AppendLine("List:");

                    var list = target.GetInvocationList();
                    for (int i = 0; i < list.Length; i++)
                    {
                        var e = list[i];
                        sb.AppendLine($"{e.Method?.DeclaringType}.{e.Method?.Name}");
                    }

                    Assert.IsTrue(true, sb.ToString());
                }
#endif
                m_Delegates[(int)t] = value;
            }
        }

#if UNITY_EDITOR
        private bool Disposed { get; set; }
#endif

        private ConditionResolver(IEventTarget owner, Hash hash)
        {
            m_Owner = owner;
            m_Hash  = hash;
        }

        void IConnector<IEventConditionProvider>.Connect(IEventConditionProvider provider)
        {
            var conditions
                = Enum.GetValues(typeof(EventCondition)).Cast<EventCondition>();
            foreach (var condition in conditions)
            {
                this[(Condition)condition] = x => provider.Resolve(condition, m_Owner, x);
            }
        }
        void IConnector<IEventConditionProvider>.Disconnect()
        {
            var conditions
                = Enum.GetValues(typeof(EventCondition)).Cast<EventCondition>();
            foreach (var condition in conditions)
            {
                this[(Condition)condition] = null;
            }
        }

        void IConnector<IStateConditionProvider>.Connect(IStateConditionProvider t)
        {
            var conditions
                = Enum.GetValues(typeof(StateCondition)).Cast<StateCondition>();
            foreach (var condition in conditions)
            {
                this[(Condition)condition] = x => t.Resolve(condition, m_Owner, x);
            }
        }
        void IConnector<IStateConditionProvider>.Disconnect()
        {
            var conditions
                = Enum.GetValues(typeof(StateCondition)).Cast<StateCondition>();
            foreach (var condition in conditions)
            {
                this[(Condition)condition] = null;
            }
        }

        public ConditionResolver Connect(IAbnormalConditionProvider provider)
        {
            Assert.IsTrue(m_Owner is IActor);

            var conditions
                = Enum.GetValues(typeof(AbnormalCondition)).Cast<AbnormalCondition>();
            foreach (var condition in conditions)
            {
                this[(Condition)condition] = x => provider.Resolve(condition, x);
            }

            return this;
        }

        public ConditionResolver Connect(IStatValueStack stats, IStatConditionProvider provider)
        {
            Assert.IsTrue(m_Owner is IActor);

            var conditions
                = Enum.GetValues(typeof(OperatorCondition)).Cast<OperatorCondition>();
            foreach (var condition in conditions)
            {
                this[(Condition)condition] = x => provider.Resolve(stats.OriginalStats, stats, condition, x);
            }
            return this;
        }

        public void Subscribe(IConditionObserver ob)
        {
            Assert.IsFalse(m_EventObservers.Contains(ob));
            m_EventObservers.Add(ob);
        }

        public void Unsubscribe(IConditionObserver ob)
        {
            m_EventObservers.Remove(ob);
        }

        public void Dispose()
        {
            Array.Clear(m_Delegates, 0, m_Delegates.Length);
            Disposed = true;
        }
    }
}