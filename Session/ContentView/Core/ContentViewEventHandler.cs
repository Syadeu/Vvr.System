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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents an event handler for ContentView events.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    internal sealed class ContentViewEventHandler<TEvent> : IContentViewEventHandler<TEvent>
        where TEvent : struct, IConvertible
    {
        /// <summary>
        /// Represents an event stack that keeps track of the event hierarchy.
        /// </summary>
        private readonly struct EventStack : IDisposable
        {
            private readonly List<TEvent> m_EventStack;

            private readonly int    m_Index;
            private readonly TEvent m_Event;

            public EventStack(List<TEvent> l, TEvent e)
            {
                m_EventStack = l;
                m_Index = m_EventStack.Count;
                m_Event = e;

                m_EventStack.Add(e);

                CheckPossibleInfiniteLoopAndThrow();

                $"Push {m_EventStack.Count}: {e}".ToLog();
            }

            [Conditional("UNITY_EDITOR")]
            private void CheckPossibleInfiniteLoopAndThrow()
            {
                int xx = m_Index,
                    yy = m_Index;

                while (true)
                {
                    xx--;
                    if (xx < 0) return;
                    yy -= 2;
                    if (yy < 0) return;

                    if (m_EventStack[xx].Equals(m_EventStack[yy]))
                        throw new InvalidOperationException("Possible infinite loop detected.");
                }
            }
            [Conditional("UNITY_EDITOR")]
            private void EvaluateEventStackAndThrow()
            {
                if (m_EventStack.Count <= m_Index)
                    throw new InvalidOperationException();
                if (m_EventStack.Count - 1 != m_Index)
                    throw new InvalidOperationException(
                        $"{m_EventStack.Count - 1} != {m_Index}, {m_Event}");
                if (!m_EventStack[m_Index].Equals(m_Event))
                    throw new InvalidOperationException();
            }

            public void Dispose()
            {
                EvaluateEventStackAndThrow();

                $"Exit {m_EventStack.Count}: {m_Event}".ToLog();
                m_EventStack.RemoveAt(m_Index);
            }
        }

        private readonly List<TEvent> m_EventStack = new();

        private readonly Dictionary<TEvent, List<uint>>                     m_Actions   = new();
        private readonly Dictionary<uint, ContentViewEventDelegate<TEvent>> m_ActionMap = new();

        private int      m_CurrentWriteThreadId;

        private readonly AsyncLocal<bool> m_ExecutionLocked = new();
        private readonly AsyncLocal<short>  m_ExecutionDepth  = new();

        private readonly SemaphoreSlim
            m_ExecutionLock = new(1, 1),
            m_WriteLock     = new(1, 1);

        private readonly CancellationTokenSource m_CancellationTokenSource = new();

        public bool Disposed { get; private set; }

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

        public IContentViewEventHandler<TEvent> Register(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

#if THREAD_DEBUG
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (m_WriteLock.CurrentCount == 0 &&
                threadId                 != m_CurrentWriteThreadId)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }
#endif
            m_WriteLock.Wait();
#if THREAD_DEBUG
            m_CurrentWriteThreadId = threadId;
#endif

            try
            {
                if (!m_Actions.TryGetValue(e, out var list))
                {
                    list         = new(8);
                    m_Actions[e] = list;
                }

                uint hash = CalculateHash(x);
                if (!m_ActionMap.TryAdd(hash, x))
                {
                    throw new InvalidOperationException("hash conflict possibly registering same method");
                }

                m_ActionMap[hash] = x;
                list.Add(hash);
            }
            finally
            {
                m_WriteLock.Release();
            }

            return this;
        }

        public IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

#if THREAD_DEBUG
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (m_WriteLock.CurrentCount == 0 &&
                threadId != m_CurrentWriteThreadId)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }
#endif
            m_WriteLock.Wait();
#if THREAD_DEBUG
            m_CurrentWriteThreadId = threadId;
#endif

            try
            {
                if (!m_Actions.TryGetValue(e, out var list)) return this;

                uint hash = CalculateHash(x);
                if (!m_ActionMap.Remove(hash)) return this;

                list.Remove(hash);
            }
            finally
            {
                m_WriteLock.Release();
            }

            return this;
        }

        public async UniTask ExecuteAsync(TEvent e) => await ExecuteAsync(e, null);
        public async UniTask ExecuteAsync(TEvent e, object ctx)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            if (!m_Actions.TryGetValue(e, out var list)) return;

            if (!m_ExecutionLocked.Value &&
                m_ExecutionDepth.Value == 0)
            {
                await m_ExecutionLock.WaitAsync();
                m_ExecutionLocked.Value = true;
            }

            m_ExecutionDepth.Value++;

            int count = list.Count;
            var array = ArrayPool<UniTask>.Shared.Rent(count);

            await m_WriteLock.WaitAsync();

            try
            {
                using var st = new EventStack(m_EventStack, e);

                for (int i = 0; i < count; i++)
                {
                    // This operation can be recursively calling this method.
                    array[i] = m_ActionMap[list[i]](e, ctx)
                        .AttachExternalCancellation(m_CancellationTokenSource.Token);
                }
            }
            finally
            {
                await UniTask.WhenAll(array);

                m_ExecutionDepth.Value--;
                if (m_ExecutionDepth.Value < 0)
                    throw new InvalidOperationException($"{m_ExecutionDepth.Value}");

                if (m_ExecutionDepth.Value == 0 && m_ExecutionLocked.Value)
                {
                    m_ExecutionLock.Release();
                    m_ExecutionLocked.Value = false;
                }

                m_WriteLock.Release();
            }

            ArrayPool<UniTask>.Shared.Return(array, true);
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(ContentViewEventHandler<TEvent>));

            m_CancellationTokenSource.Cancel();

            foreach (var list in m_Actions.Values)
            {
                list.Clear();
            }

            m_Actions.Clear();
            m_ActionMap.Clear();
            Disposed = true;
        }
    }
}