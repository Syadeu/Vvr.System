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
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents an event handler for ContentView events.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    public sealed class ContentViewEventHandler<TEvent> : IContentViewEventHandler<TEvent>
        where TEvent : struct, IConvertible
    {
        private struct WriteLock : IDisposable
        {
            private readonly ContentViewEventHandler<TEvent> m_Handler;
            private          bool                            m_Started;

            public WriteLock(ContentViewEventHandler<TEvent> h)
            {
                m_Handler = h;
                m_Started = true;
            }

            public async UniTask WaitAsync()
            {
                if (!m_Handler.m_WriteLocked.Value)
                {
                    await m_Handler.m_WriteLock.WaitAsync();
                    m_Handler.m_WriteLocked.Value = true;
                }
            }
            public void Wait()
            {
#if THREAD_DEBUG
                int threadId = Thread.CurrentThread.ManagedThreadId;
                if (m_Handler.m_WriteLock.CurrentCount == 0 &&
                    threadId                           != m_Handler.m_CurrentWriteThreadId)
                {
                    if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                        throw new InvalidOperationException(
                            "Another thread currently writing but main thread trying to write." +
                            "This is not allowed main thread should not be awaited.");
                }
#endif
                if (!m_Handler.m_WriteLocked.Value)
                {
                    m_Handler.m_WriteLock.Wait();
                    m_Handler.m_WriteLocked.Value = true;
                }
#if THREAD_DEBUG
                Interlocked.Exchange(ref m_Handler.m_CurrentWriteThreadId, threadId);
#endif
            }

            public void Dispose()
            {
                if (!m_Started)
                {
                    return;
                }

                if (m_Handler.m_WriteLocked.Value)
                {
                    m_Handler.m_WriteLock.Release();
                    m_Handler.m_WriteLocked.Value = false;

                    // $"write lock out".ToLog();
                }
            }
        }
        private struct ExecutionLock : IDisposable
        {
            private readonly ContentViewEventHandler<TEvent> m_Handler;

            public bool Started { get; private set; }

            public ExecutionLock(ContentViewEventHandler<TEvent> h)
            {
                m_Handler = h;
                Started   = true;
            }

            public async UniTask WaitAsync()
            {
                if (!m_Handler.m_ExecutionLocked.Value
                    && m_Handler.m_ExecutionDepth.Value == 0
                    )
                {
                    // $"execution lock in {m_Handler.m_ExecutionDepth.Value}".ToLog();
                    await m_Handler.m_ExecutionLock.WaitAsync();

                    m_Handler.m_ExecutionLocked.Value = true;
                }

                m_Handler.m_ExecutionDepth.Value += 1;
                // $"in {m_Handler.m_ExecutionDepth.Value}, {m_Handler.m_ExecutionLocked.Value}".ToLog();

                Started = true;
            }

            public void Dispose()
            {
                if (!Started)
                {
                    return;
                }

                // $"depth: {m_Handler.m_ExecutionDepth.Value}".ToLog();
                m_Handler.m_ExecutionDepth.Value -= 1;
                if (m_Handler.m_ExecutionDepth.Value < 0)
                    throw new InvalidOperationException($"{m_Handler.m_ExecutionDepth.Value}");

                if (
                    m_Handler.m_ExecutionDepth.Value == 0 &&
                    m_Handler.m_ExecutionLocked.Value)
                {
                    m_Handler.m_ExecutionLock.Release();
                    m_Handler.m_ExecutionLocked.Value = false;

                    // $"execution lock out {m_Handler.m_ExecutionDepth.Value}".ToLog();
                }
                // else $"depth: {m_Handler.m_ExecutionDepth} exit, {m_Handler.m_ExecutionLocked == 1}".ToLog();
                // else $"depth: {m_Handler.m_ExecutionDepth.Value} exit, {m_Handler.m_ExecutionLocked.Value}".ToLog();
            }
        }

        private readonly Dictionary<TEvent, List<uint>>                     m_Actions   = new();
        private readonly ConcurrentDictionary<uint, ContentViewEventDelegate<TEvent>> m_ActionMap = new();

#if THREAD_DEBUG
        private int m_CurrentWriteThreadId;
#endif
        private readonly AsyncLocal<int>  m_ExecutionDepth  = new();
        private readonly AsyncLocal<bool>
            m_WriteLocked = new(),
            m_ExecutionLocked = new();

        private readonly SemaphoreSlim
            m_ExecutionLock = new(1, 1),
            m_WriteLock     = new(1, 1);

        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        public bool Disposed { get; private set; }

        public bool ExecutionLocked => m_ExecutionLock.CurrentCount == 0;
        public bool WriteLocked => m_WriteLock.CurrentCount == 0;

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

        public void Clear()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            using var writeLock = new WriteLock(this);
            writeLock.Wait();

            foreach (var list in m_Actions.Values)
            {
                list.Clear();
            }

            m_Actions.Clear();
            m_ActionMap.Clear();
        }

        public IContentViewEventHandler<TEvent> Register(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            using var writeLock = new WriteLock(this);
            writeLock.Wait();

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

        public IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            using var writeLock = new WriteLock(this);
            writeLock.Wait();

            if (!m_Actions.TryGetValue(e, out var list)) return this;

            uint hash = CalculateHash(x);
            if (!m_ActionMap.TryRemove(hash, out _)) return this;

            list.Remove(hash);

            return this;
        }

        public async UniTask ExecuteAsync(TEvent e) => await ExecuteAsync(e, null);
        public async UniTask ExecuteAsync(TEvent e, object ctx)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            ExecutionLock executionLock = new ExecutionLock(this);
            await executionLock.WaitAsync();

            Assert.IsTrue(executionLock.Started);

            if (!m_Actions.TryGetValue(e, out var list)) return;
            int count = list.Count;
            var array = ArrayPool<UniTask>.Shared.Rent(count);

            // using (new EventStack(m_EventStack, e))
            {
                int i = 0;
                // using (var writeLock = new WriteLock(this))
                {
                    // writeLock.Wait();

                    for (; i < count; i++)
                    {
                        // This operation can be recursively calling this method.
                        if (!m_ActionMap.TryGetValue(list[i], out var target))
                            continue;

                        array[i] = target(e, ctx)
                            .AttachExternalCancellation(m_CancellationTokenSource.Token);
                    }
                }

                for (; i < array.Length; i++)
                {
                    array[i] = UniTask.CompletedTask;
                }

                // Because thread can be changed after yield.
                // Executions are protected by TLS,
                // so make sure stacks after all execute has been queued.
                executionLock.Dispose();

                await UniTask.WhenAll(array);
            }

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