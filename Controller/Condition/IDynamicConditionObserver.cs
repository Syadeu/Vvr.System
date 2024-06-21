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
// File created : 2024, 05, 13 15:05

#endregion

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Condition
{
    [PublicAPI]
    public delegate UniTask ConditionObserverDelegate(IEventTarget owner, string value, CancellationToken cancellationToken);

    [PublicAPI]
    public interface IDynamicConditionObserver : IDisposable
    {
        [ThreadSafe]
        ConditionObserverDelegate this[Model.Condition t] { get; set; }

        [ThreadSafe]
        bool Disposed { get; }

        UniTask WaitForCondition(Model.Condition condition, CancellationToken cancellationToken = default);
    }

    internal sealed class DynamicConditionObserver : IConditionObserver, IDynamicConditionObserver
    {
        public static readonly ConditionObserverDelegate None = (_, _, _) => UniTask.CompletedTask;

        private ConditionResolver m_Parent;

        private ConditionQuery              m_Filter;
        private ConditionObserverDelegate[] m_Delegates;

        private readonly SemaphoreSlim           m_WriteLock               = new(1, 1);
        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        private int m_Disposed;

        public ConditionObserverDelegate this[Model.Condition t]
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ConditionResolver));

                using var wl = new SemaphoreSlimLock(m_WriteLock);
                wl.Wait(TimeSpan.FromSeconds(1));

                if (m_Delegates is null ||
                    !m_Filter.Has(t))
                {
                    return null;
                }

                int i = m_Filter.IndexOf(t);
                return m_Delegates[i];
            }
            set
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(ConditionResolver));

                using var wl = new SemaphoreSlimLock(m_WriteLock);
                wl.Wait(TimeSpan.FromSeconds(1));

                var modifiedQuery  = m_Filter | t;
                int modifiedLength = modifiedQuery.MaxIndex + 1;

                // require resize
                if (m_Delegates is null || m_Delegates.Length < modifiedLength)
                {
                    var nArr = ArrayPool<ConditionObserverDelegate>.Shared.Rent(modifiedLength);

                    if (m_Delegates is not null)
                    {
                        foreach (var condition in m_Filter)
                        {
                            nArr[modifiedQuery.IndexOf(condition)] = m_Delegates[m_Filter.IndexOf(condition)];
                        }

                        ArrayPool<ConditionObserverDelegate>.Shared.Return(m_Delegates, true);
                    }

                    m_Delegates = nArr;
                }

                if (!m_Filter.IsEmpty && (short)t < (short)m_Filter.First)
                {
                    for (int j = 0; j < m_Delegates.Length - 1; j++)
                    {
                        m_Delegates[j + 1] = m_Delegates[j];
                    }

                    m_Delegates[0] = value;
                    m_Filter       = modifiedQuery;
                    return;
                }

                m_Filter = modifiedQuery;
                int i = m_Filter.IndexOf(t);

                m_Delegates[i] = value;
            }
        }

        public bool Disposed => m_Disposed == 1;

        ConditionQuery IConditionObserver.Filter => m_Filter;

        internal DynamicConditionObserver(ConditionResolver r)
        {
            m_Parent = r;
        }

        async UniTask IConditionObserver.OnExecute(Model.Condition condition, string value, CancellationToken cancellationToken)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            if (m_Parent.Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            using var wl = new SemaphoreSlimLock(m_WriteLock);
            await wl.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);

            int i = m_Filter.IndexOf(condition);
            Assert.IsFalse(i < 0);

            if (m_Delegates[i] == null)
                throw new InvalidOperationException($"{condition} not found with value({value}), {m_Filter.ToString()}");

            using var cts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_CancellationTokenSource.Token);
            await m_Delegates[i](m_Parent.Owner, value, cts.Token)
                    .AttachExternalCancellation(cts.Token)
                ;
        }

        public async UniTask WaitForCondition(Model.Condition condition, CancellationToken cancellationToken = default)
        {
            bool executed = false;
            ConditionObserverDelegate d = (owner, value, token) =>
            {
                executed = true;
                return UniTask.CompletedTask;
            };
            this[condition] += d;

            while (!executed)
            {
                await UniTask.Yield();

                if (cancellationToken is { CanBeCanceled: true, IsCancellationRequested: true })
                    break;
            }

            this[condition] -= d;
        }

        public void Dispose()
        {
            if (m_WriteLock.CurrentCount is 0)
                throw new InvalidOperationException();
            if (Interlocked.Exchange(ref m_Disposed, 1) is not 0)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            m_CancellationTokenSource.Cancel();
            m_CancellationTokenSource.Dispose();

            if (!m_Parent.Disposed)
                m_Parent.Unsubscribe(this);

            if (m_Delegates != null)
                ArrayPool<ConditionObserverDelegate>.Shared.Return(m_Delegates, true);
            m_Parent = null;
        }
    }
}