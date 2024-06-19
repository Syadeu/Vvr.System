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

        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        private int m_Disposed;

        public bool              Disposed          => m_Disposed == 1;
        public CancellationToken CancellationToken => m_CancellationTokenSource.Token;

        public bool WriteLocked => WriteLock.CurrentCount == 0;

        protected SemaphoreSlim    WriteLock       { get; } = new(1, 1);
        protected AsyncLocal<bool> TaskWriteLocked { get; } = new();

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

            var  writeLock = new SemaphoreSlimLock(WriteLock);
            bool wasLocked = TaskWriteLocked.Value;
            if (!wasLocked)
            {
                TaskWriteLocked.Value = true;
                writeLock.Wait(CancellationToken);
            }

            foreach (var list in m_Actions.Values)
            {
                list.Clear();
            }

            m_Actions.Clear();
            m_ActionMap.Clear();

            if (!wasLocked)
            {
                writeLock.Dispose();
                TaskWriteLocked.Value = false;
            }
        }

        private void InternalRegister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (CancellationToken.IsCancellationRequested) return;

            if (!m_Actions.TryGetValue(e, out var list))
            {
                list         = new(8);
                m_Actions[e] = list;
            }

            uint hash = CalculateHash(x);
            if (list.Contains(hash))
            {
                throw new InvalidOperationException("hash conflict possibly registering same method");
            }

            m_ActionMap[hash] = x;
            list.Add(hash);
        }
        private void InternalUnregister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (CancellationToken.IsCancellationRequested) return;

            if (!m_Actions.TryGetValue(e, out var list)) return;

            uint hash = CalculateHash(x);
            if (!m_ActionMap.TryRemove(hash, out _)) return;

            list.Remove(hash);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public virtual IContentViewEventHandler<TEvent> Register(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));
            // if (m_ExecuteEntered == 1)
            //     throw new InvalidOperationException("Cannot register while executing");

            var  writeLock   = new SemaphoreSlimLock(WriteLock, true);
            bool writeLocked = TaskWriteLocked.Value;
            if (!writeLocked)
            {
                TaskWriteLocked.Value = true;
                writeLock.Wait(TimeSpan.FromSeconds(1), CancellationToken);
            }

            try
            {
                InternalRegister(e, x);
            }
            finally
            {
                if (!writeLocked)
                {
                    TaskWriteLocked.Value = false;
                    writeLock.Dispose();
                }
            }

            return this;
        }
        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public virtual IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));
            // if (m_ExecuteEntered == 1)
            //     throw new InvalidOperationException("Cannot unregister while executing");

            var  writeLock   = new SemaphoreSlimLock(WriteLock, true);
            bool writeLocked = TaskWriteLocked.Value;
            if (!writeLocked)
            {
                TaskWriteLocked.Value = true;
                writeLock.Wait(TimeSpan.FromSeconds(1), CancellationToken);
            }

            try
            {
                InternalUnregister(e, x);
            }
            finally
            {
                if (!writeLocked)
                {
                    TaskWriteLocked.Value = false;
                    writeLock.Dispose();
                }
            }

            return this;
        }

        public async UniTask ExecuteAsync(TEvent e) => await ExecuteAsync(e, null);
        public virtual async UniTask ExecuteAsync(TEvent e, object ctx)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            SemaphoreSlimLock wl = new SemaphoreSlimLock(WriteLock);

            bool writeLocked  = TaskWriteLocked.Value;
            if (!writeLocked)
            {
                TaskWriteLocked.Value = true;
                await wl.WaitAsync(CancellationToken);
            }

            if (!CancellationToken.IsCancellationRequested &&
                m_Actions.TryGetValue(e, out var list))
            {
                int       count     = list.Count;
                using var tempArray = TempArray<UniTask>.Shared(count, true);

                int i = 0;
                for (; i < count; i++)
                {
                    // This operation can be recursively calling this method.
                    if (!m_ActionMap.TryGetValue(list[i], out var target))
                        continue;

                    tempArray.Value[i] = target(e, ctx)
                        .AttachExternalCancellation(m_CancellationTokenSource.Token);
                }

                for (; i < tempArray.Value.Length; i++)
                {
                    tempArray.Value[i] = UniTask.CompletedTask;
                }

                // Because thread can be changed after yield.
                // Executions are protected by TLS,
                // so make sure stacks after all execute has been queued.
                if (!writeLocked)
                {
                    TaskWriteLocked.Value = false;
                    wl.Dispose();
                }

                await UniTask.WhenAll(tempArray.Value)
                    .AttachExternalCancellation(m_CancellationTokenSource.Token);
            }
            else
            {
                if (!writeLocked)
                {
                    TaskWriteLocked.Value = false;
                    wl.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_Disposed, 1) == 1)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            m_CancellationTokenSource.Cancel();
            Clear();
        }
    }
}