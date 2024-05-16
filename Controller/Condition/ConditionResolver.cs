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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Condition
{
    public delegate bool ConditionDelegate(string value);

    public sealed class ConditionResolver : IReadOnlyConditionResolver, IDisposable,
        IConnector<IEventConditionProvider>,
        IConnector<IStateConditionProvider>
    {
        public static readonly ConditionDelegate Always = _ => true;
        public static readonly ConditionDelegate False = _ => false;

        public static ConditionResolver Create(IEventTarget o, IReadOnlyConditionResolver parent)
        {
            return new(o, parent);
        }
        internal static async UniTask Execute(IEventTarget o, Model.Condition condition, string v)
        {
            if (o is not IConditionTarget target) return;

            var resolver = (ConditionResolver)target.ConditionResolver;
            if (resolver.m_Parent != null)
            {
                await Execute(resolver.m_Parent.Owner, condition, v);
            }

            for (int i = 0; i < resolver.m_EventObservers.Count; i++)
            {
                var e = resolver.m_EventObservers[i];
                if (!e.Filter.Has(condition)) continue;

                await e.OnExecute(condition, v);
            }
        }

        private readonly IReadOnlyConditionResolver m_Parent;

        private ConditionQuery      m_Filter;
        private ConditionDelegate[] m_Delegates;

        private readonly List<IConditionObserver> m_EventObservers = new();

        public ConditionDelegate this[Model.Condition t]
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ConditionResolver));
                if (t == 0) return Always;

                if (m_Delegates == null ||
                    !m_Filter.Has(t))
                {
                    $"[Condition] Condition {t} is not connected.".ToLog();
                    return False;
                }

                int i = m_Filter.IndexOf(t);
                return m_Delegates[i];
            }
            set
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ConditionResolver));
                if (t == 0) throw new InvalidOperationException("You are trying to override Always condition.");

                var modifiedQuery  = m_Filter | t;
                int modifiedLength = modifiedQuery.MaxIndex + 1;

                // require resize
                if (m_Delegates == null || m_Delegates.Length < modifiedLength)
                {
                    ConditionDelegate[] nArr = ArrayPool<ConditionDelegate>.Shared.Rent(modifiedLength);

                    if (m_Delegates != null)
                    {
                        foreach (var condition in m_Filter)
                        {
                            nArr[modifiedQuery.IndexOf(condition)] = m_Delegates[m_Filter.IndexOf(condition)];
                        }
                        ArrayPool<ConditionDelegate>.Shared.Return(m_Delegates, true);
                    }

                    m_Delegates = nArr;
                }

                m_Filter = modifiedQuery;
                int i = m_Filter.IndexOf(t);

                if (value != null)
                {
                    var target = m_Delegates[i];
                    if (target != null && target.GetInvocationList().Length > 0)
                    {
                        StringBuilder sb = new();
                        sb.AppendLine("Chaining condition will leads unexpected result.");
                        sb.Append("List: ");
                        sb.Append($"{value.Method?.DeclaringType}.{value.Method?.Name}");

                        var list = target.GetInvocationList();
                        for (int j = 0; j < list.Length; j++)
                        {
                            var e = list[j];
                            sb.AppendLine($"{e.Method?.DeclaringType}.{e.Method?.Name}");
                        }

                        throw new InvalidOperationException(sb.ToString());
                    }
                }

                m_Delegates[i] = value;
            }
        }

        private bool Connected { get; set; }
        [PublicAPI]
        public bool Disposed  { get; private set; }

        public IEventTarget Owner { get; }

        public ConditionResolver(IEventTarget owner)
        {
            Owner = owner;
        }
        private ConditionResolver(IEventTarget owner, IReadOnlyConditionResolver parent)
            : this(owner)
        {
            m_Parent = parent;
        }

        public ConditionResolver Connect()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            Vvr.Provider.Provider.Static
                .Connect<IEventConditionProvider>(this)
                .Connect<IStateConditionProvider>(this);

            Connected = true;
            return this;
        }
        public ConditionResolver Disconnect()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            Vvr.Provider.Provider.Static
                .Disconnect<IEventConditionProvider>(this)
                .Disconnect<IStateConditionProvider>(this);
            Connected = false;
            return this;
        }

        void IConnector<IEventConditionProvider>.Connect(IEventConditionProvider provider)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            var conditions
                = Enum.GetValues(typeof(EventCondition)).Cast<EventCondition>();
            foreach (var condition in conditions)
            {
                if (condition == 0) continue;

                this[(Model.Condition)condition] = x => provider.Resolve(condition, Owner, x);
            }
        }
        void IConnector<IEventConditionProvider>.Disconnect(IEventConditionProvider provider)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            var conditions
                = Enum.GetValues(typeof(EventCondition)).Cast<EventCondition>();
            foreach (var condition in conditions)
            {
                if (condition == 0) continue;

                this[(Model.Condition)condition] = null;
            }
        }

        void IConnector<IStateConditionProvider>.Connect(IStateConditionProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            var conditions
                = Enum.GetValues(typeof(StateCondition)).Cast<StateCondition>();
            foreach (var condition in conditions)
            {
                if (condition == 0) continue;

                this[(Model.Condition)condition] = x => t.Resolve(condition, Owner, x);
            }
        }
        void IConnector<IStateConditionProvider>.Disconnect(IStateConditionProvider t)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            var conditions
                = Enum.GetValues(typeof(StateCondition)).Cast<StateCondition>();
            foreach (var condition in conditions)
            {
                if (condition == 0) continue;

                this[(Model.Condition)condition] = null;
            }
        }

        public ConditionResolver Connect(IAbnormalConditionProvider provider)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));
            Assert.IsTrue(Owner is IActor);

            var conditions
                = Enum.GetValues(typeof(AbnormalCondition)).Cast<AbnormalCondition>();
            foreach (var condition in conditions)
            {
                if (condition == 0) continue;

                this[(Model.Condition)condition] = x => provider.Resolve(condition, x);
            }

            return this;
        }

        public ConditionResolver Connect(IStatValueStack stats, IStatConditionProvider provider)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));
            Assert.IsTrue(Owner is IActor);

            var conditions
                = Enum.GetValues(typeof(OperatorCondition)).Cast<OperatorCondition>();
            foreach (var condition in conditions)
            {
                if (condition == 0) continue;

                this[(Model.Condition)condition] = x => provider.Resolve(stats.OriginalStats, stats, condition, x);
            }
            return this;
        }

        public IReadOnlyConditionResolver Subscribe(IConditionObserver ob)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            Assert.IsFalse(m_EventObservers.Contains(ob));
            m_EventObservers.Add(ob);
            return this;
        }
        public IReadOnlyConditionResolver Unsubscribe(IConditionObserver ob)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            m_EventObservers.Remove(ob);
            return this;
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            if (Connected) Disconnect();
            if (m_Delegates != null)
                ArrayPool<ConditionDelegate>.Shared.Return(m_Delegates, true);
            Disposed = true;
        }
    }

    public static class ConditionResolverExtensions
    {
        public static IDynamicConditionObserver CreateObserver(
            [NotNull] this IReadOnlyConditionResolver t)
        {
            Assert.IsNotNull(t);
            ConditionResolver r = (ConditionResolver)t;

            DynamicConditionObserver ob = new(r);
            t.Subscribe(ob);
            return ob;
        }
    }
}