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
using System.Threading;
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

    [PublicAPI]
    public sealed class ConditionResolver : IReadOnlyConditionResolver, IDisposable
    {
        public static readonly ConditionDelegate Always = _ => true;
        public static readonly ConditionDelegate False = _ => false;

        /// <summary>
        /// Create new condition resolver with parent.
        /// </summary>
        /// <remarks>
        /// If target condition cannot be resolved by itself, resolve with parent's resolver.
        /// </remarks>
        /// <param name="o"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static ConditionResolver Create(IEventTarget o, IReadOnlyConditionResolver parent)
        {
            return new(o, parent);
        }
        internal static async UniTask Execute(
            IEventTarget o, Model.Condition condition, string v, CancellationToken cancellationToken)
        {
            if (o is not IConditionTarget target) return;

            // TODO: ocp
            var resolver = (ConditionResolver)target.ConditionResolver;
            if (resolver.m_Parent != null)
            {
                await Execute(resolver.m_Parent.Owner, condition, v, cancellationToken);
            }

            using var cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                    resolver.m_CancellationTokenSource.Token);
            using var tempArray = TempArray<UniTask>.Shared(resolver.m_EventObservers.Count, true);
            for (int i = 0; i < resolver.m_EventObservers.Count; i++)
            {
                var e = resolver.m_EventObservers[i];
                if (!e.Filter.Has(condition)) continue;

                tempArray.Value[i] = e.OnExecute(condition, v, cancellationTokenSource.Token);
            }

            await UniTask.WhenAll(tempArray.Value).AttachExternalCancellation(cancellationTokenSource.Token);
        }

        private readonly IReadOnlyConditionResolver m_Parent;

        private ConditionQuery      m_Filter;
        private ConditionDelegate[] m_Delegates;
        private int                 m_Disposed = 0;

        private CancellationTokenSource m_CancellationTokenSource = new();

        private readonly List<IConditionObserver> m_EventObservers = new();

        private readonly SemaphoreSlim m_WriteLock = new(1, 1);

        public ConditionQuery Filter => m_Filter;
        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public ConditionDelegate this[Model.Condition t]
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ConditionResolver));
                if (t is 0) return Always;

                using var wl = new SemaphoreSlimLock(m_WriteLock);
                wl.Wait(TimeSpan.FromSeconds(1));

                if (m_Delegates is null ||
                    !m_Filter.Has(t))
                {
                    // If there is no parent, this resolver cannot resolve target condition
                    if (m_Parent == null)
                        throw new InvalidOperationException($"[Condition] Condition {t} is not connected.");

                    // Use parent's condition resolver.
                    return m_Parent[t];
                }

                int i = m_Filter.IndexOf(t);
                return m_Delegates[i];
            }
            set
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ConditionResolver));
                if (t is 0) throw new InvalidOperationException("You are trying to override Always condition.");

                using var wl = new SemaphoreSlimLock(m_WriteLock);
                wl.Wait(TimeSpan.FromSeconds(1));

                var modifiedQuery  = m_Filter | t;
                int modifiedLength = modifiedQuery.MaxIndex + 1;

                // require resize
                if (m_Delegates is null || m_Delegates.Length < modifiedLength)
                {
                    ConditionDelegate[] nArr = ArrayPool<ConditionDelegate>.Shared.Rent(modifiedLength);

                    if (m_Delegates is not null)
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

                if (value is not null)
                {
                    var target = m_Delegates[i];
                    if (target is not null && target.GetInvocationList().Length > 0)
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

        [ThreadSafe]
        public bool Disposed => m_Disposed == 1;

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

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public bool CanResolve(Model.Condition t)
        {
            if (t == 0) return true;

            using var wl = new SemaphoreSlimLock(m_WriteLock);
            wl.Wait(TimeSpan.FromSeconds(1));

            if (m_Delegates is null ||
                !m_Filter.Has(t))
            {
                // If there is no parent, this resolver cannot resolve target condition
                if (m_Parent == null)
                    return false;

                // Use parent's condition resolver.
                return m_Parent.CanResolve(t);
            }

            return true;
        }

        void IConnector<IEventConditionProvider>.Connect(IEventConditionProvider provider)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            var conditions
                = Enum.GetValues(typeof(EventCondition)).Cast<EventCondition>();
            foreach (var condition in conditions)
            {
                if (condition is 0) continue;

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
                if (condition is 0) continue;

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
                if (condition is 0) continue;

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
                if (condition is 0) continue;

                this[(Model.Condition)condition] = null;
            }
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public ConditionResolver Connect([NotNull] IAbnormalConditionProvider provider)
        {
            Assert.IsNotNull(provider);
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));
            Assert.IsTrue(Owner is IActor);

            var conditions
                = Enum.GetValues(typeof(AbnormalCondition)).Cast<AbnormalCondition>();
            foreach (var condition in conditions)
            {
                if (condition is 0) continue;

                this[(Model.Condition)condition] = x => provider.Resolve(condition, x);
            }

            return this;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public ConditionResolver Connect([NotNull] IStatValueStack stats, [NotNull] IStatConditionProvider provider)
        {
            Assert.IsNotNull(stats);
            Assert.IsNotNull(provider);
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));
            Assert.IsTrue(Owner is IActor);

            var conditions
                = Enum.GetValues(typeof(OperatorCondition)).Cast<OperatorCondition>();
            foreach (var condition in conditions)
            {
                if (condition is 0) continue;

                this[(Model.Condition)condition] = x => provider.Resolve(stats.OriginalStats, stats, condition, x);
            }
            return this;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public IReadOnlyConditionResolver Subscribe([NotNull] IConditionObserver ob)
        {
            Assert.IsNotNull(ob);
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            using var wl = new SemaphoreSlimLock(m_WriteLock);
            wl.Wait(TimeSpan.FromSeconds(1));

            Assert.IsFalse(m_EventObservers.Contains(ob));
            m_EventObservers.Add(ob);
            return this;
        }
        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public IReadOnlyConditionResolver Unsubscribe([NotNull] IConditionObserver ob)
        {
            Assert.IsNotNull(ob);
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            using var wl = new SemaphoreSlimLock(m_WriteLock);
            wl.Wait(TimeSpan.FromSeconds(1));

            m_EventObservers.Remove(ob);
            return this;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void Clear()
        {
            using var wl = new SemaphoreSlimLock(m_WriteLock);
            wl.Wait(TimeSpan.FromSeconds(1));

            m_CancellationTokenSource.Cancel();
            m_CancellationTokenSource.Dispose();
            m_CancellationTokenSource = new();

            for (int i = 0; i < m_Delegates?.Length; i++)
            {
                m_Delegates[i] = null;
            }

            m_EventObservers.Clear();
        }

        public void Dispose()
        {
            if (m_WriteLock.CurrentCount == 0)
                throw new InvalidOperationException();

            if (Interlocked.Exchange(ref m_Disposed, 1) != 0)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            m_CancellationTokenSource.Cancel();
            m_CancellationTokenSource.Dispose();

            if (m_Delegates != null)
                ArrayPool<ConditionDelegate>.Shared.Return(m_Delegates, true);
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