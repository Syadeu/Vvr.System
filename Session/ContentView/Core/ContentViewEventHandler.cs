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
// File created : 2024, 06, 02 21:06

#endregion

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define THREAD_DEBUG
#endif

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents an event handler for ContentView events.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    public class ContentViewEventHandler<TEvent> : IContentViewEventHandler<TEvent>
        where TEvent : struct, IConvertible
    {
        private readonly Dictionary<TEvent, List<uint>> m_Actions = new();
        private readonly ConcurrentDictionary<uint, ContentViewEventDelegate<TEvent>> m_ActionMap = new();

#if THREAD_DEBUG
        private int m_CurrentWriteThreadId;
#endif
        private readonly AsyncLocal<int>  m_ExecutionDepth  = new();

        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        public bool              Disposed          { get; private set; }
        public CancellationToken CancellationToken => m_CancellationTokenSource.Token;

        public bool ExecutionLocked => ExecutionLock.CurrentCount == 0;
        public bool WriteLocked => WriteLock.CurrentCount == 0;

        protected SemaphoreSlim ExecutionLock { get; } = new(1, 1);
        protected SemaphoreSlim WriteLock       { get; } = new(1, 1);

        protected AsyncLocal<int> ExecutionDepth    => m_ExecutionDepth;

        [Pure]
        private static uint CalculateHash(ContentViewEventDelegate<TEvent> x)
        {
            uint hash =
                FNV1a32.Calculate(x.Method.Name)
                ^ 267;
            if (x.Method.DeclaringType is not null)
                hash ^= FNV1a32.Calculate(x.Method.DeclaringType.FullName);
            if (x.Target is not null)
                hash ^= unchecked((uint)x.Target.GetHashCode());

            return hash;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public virtual void Clear()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            using var writeLock = new SemaphoreSlimLock(WriteLock);
            writeLock.Wait(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return;

            foreach (var list in m_Actions.Values)
            {
                list.Clear();
            }

            m_Actions.Clear();
            m_ActionMap.Clear();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public virtual IContentViewEventHandler<TEvent> Register(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            using var writeLock = new SemaphoreSlimLock(WriteLock, true);
            writeLock.Wait(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return this;

            if (!m_Actions.TryGetValue(e, out var list))
            {
                list         = new(8);
                m_Actions[e] = list;
            }

            uint hash = CalculateHash(x);
            if (list.Contains(hash))
                throw new InvalidOperationException("hash conflict possibly registering same method");

            m_ActionMap[hash] = x;
            list.Add(hash);

            return this;
        }
        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public virtual IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            using var writeLock = new SemaphoreSlimLock(WriteLock, true);
            writeLock.Wait(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return this;

            if (!m_Actions.TryGetValue(e, out var list)) return this;

            uint hash = CalculateHash(x);
            if (!m_ActionMap.TryRemove(hash, out _)) return this;

            list.Remove(hash);

            return this;
        }

        public async UniTask ExecuteAsync(TEvent e) => await ExecuteAsync(e, null);
        public virtual async UniTask ExecuteAsync(TEvent e, object ctx)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            SemaphoreSlimLock wl = new SemaphoreSlimLock(WriteLock);
            SemaphoreSlimLock l  = new SemaphoreSlimLock(ExecutionLock);
            if (m_ExecutionDepth.Value++ == 0)
            {
                await wl.WaitAsync(CancellationToken);
                await l.WaitAsync(CancellationToken);
            }
            // ExecutionLock executionLock = new ExecutionLock(this);
            // await executionLock.WaitAsync(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return;
            // Assert.IsTrue(executionLock.Started);

            if (!m_Actions.TryGetValue(e, out var list)) return;
            int count = list.Count;
            var array = ArrayPool<UniTask>.Shared.Rent(count);

            int i = 0;
            for (; i < count; i++)
            {
                // This operation can be recursively calling this method.
                if (!m_ActionMap.TryGetValue(list[i], out var target))
                    continue;

                array[i] = target(e, ctx)
                    .AttachExternalCancellation(m_CancellationTokenSource.Token);
            }

            for (; i < array.Length; i++)
            {
                array[i] = UniTask.CompletedTask;
            }

            // Because thread can be changed after yield.
            // Executions are protected by TLS,
            // so make sure stacks after all execute has been queued.
            // executionLock.Dispose();
            if (--m_ExecutionDepth.Value == 0)
            {
                wl.Dispose();
                l.Dispose();
            }
            else if (m_ExecutionDepth.Value < 0)
                throw new InvalidOperationException();

            $"{m_ExecutionDepth.Value}".ToLog();

            await UniTask.WhenAll(array)
                .AttachExternalCancellation(m_CancellationTokenSource.Token);

            ArrayPool<UniTask>.Shared.Return(array, true);
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            m_CancellationTokenSource.Cancel();
            Clear();

            Disposed = true;
        }
    }
}