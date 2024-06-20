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
// File created : 2024, 06, 12 18:06

#endregion

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define THREAD_DEBUG
#endif

using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Model
{
    /// <summary>
    /// Represents a lightweight mutual exclusion mechanism that limits the number of concurrent accesses to a resource.
    /// </summary>
    /// <remarks>
    /// This lock has a limitation and should not be used when the method can be called recursively.
    /// </remarks>
    [PublicAPI]
    public struct SemaphoreSlimLock : IDisposable
    {
        private readonly SemaphoreSlim m_SemaphoreSlim;

        private bool m_AllowMainThread;
        private int  m_CurrentThreadId;
        private int  m_Started;

        public SemaphoreSlimLock(SemaphoreSlim s, bool allowMainThread = true)
        {
            m_SemaphoreSlim   = s;
            m_AllowMainThread = allowMainThread;
            m_CurrentThreadId = 0;
            m_Started         = 0;
        }

        /// <summary>
        /// Asynchronously waits for the lock to be released.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the lock has already been acquired.</exception>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

            if (!isCurrentWriteThread)
                return m_SemaphoreSlim.WaitAsync(cancellationToken);

            return Task.CompletedTask;
        }
        public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

            if (!isCurrentWriteThread)
                return m_SemaphoreSlim.WaitAsync(timeout, cancellationToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously waits for the lock to be released.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the lock to be released.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the lock has already been acquired.</exception>
        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

            if (!isCurrentWriteThread)
            {
                return m_SemaphoreSlim.WaitAsync(timeout);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Waits for the lock to be released.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <exception cref="InvalidOperationException">Thrown when the lock has already been acquired.</exception>
        public void Wait(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

#if THREAD_DEBUG
            if (!m_AllowMainThread && m_SemaphoreSlim.CurrentCount == 0 &&
                !isCurrentWriteThread)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }

            RealtimeTimer t = RealtimeTimer.Start();
#endif

            if (!isCurrentWriteThread)
                m_SemaphoreSlim.Wait(cancellationToken);

#if UNITY_EDITOR
            if (m_AllowMainThread &&
                !isCurrentWriteThread &&
                UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                $"Main thread has been awaited({t.ElapsedTime.TotalSeconds}s) with {nameof(SemaphoreSlimLock)}".ToLog();
            }
#endif
        }

        /// <summary>
        /// Waits for the lock to be released.
        /// </summary>
        /// <param name="timeout">The time span to wait before timing out.</param>
        /// <exception cref="InvalidOperationException">Thrown when the lock has already been acquired.</exception>
        /// <exception cref="TimeoutException">Thrown when the timeout period has elapsed before the lock is acquired.</exception>
        public void Wait(TimeSpan timeout)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

#if THREAD_DEBUG
            if (!m_AllowMainThread && m_SemaphoreSlim.CurrentCount == 0 &&
                !isCurrentWriteThread)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }

            RealtimeTimer t = RealtimeTimer.Start();
#endif

            if (!isCurrentWriteThread)
                if (!m_SemaphoreSlim.Wait(timeout))
                    throw new TimeoutException();

#if UNITY_EDITOR
            if (m_AllowMainThread     &&
                !isCurrentWriteThread &&
                UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                $"Main thread has been awaited({t.ElapsedTime.TotalSeconds}s) with {nameof(SemaphoreSlimLock)}".ToLog();
            }
#endif
        }
        public void Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

#if THREAD_DEBUG
            if (!m_AllowMainThread && m_SemaphoreSlim.CurrentCount == 0 &&
                !isCurrentWriteThread)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }

            RealtimeTimer t = RealtimeTimer.Start();
#endif

            if (!isCurrentWriteThread)
                if (!m_SemaphoreSlim.Wait(timeout, cancellationToken))
                    throw new TimeoutException();

#if UNITY_EDITOR
            if (m_AllowMainThread     &&
                !isCurrentWriteThread &&
                UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                if (.1f <= t.ElapsedTime.TotalSeconds)
                    $"Main thread has been awaited({t.ElapsedTime.TotalSeconds}s) with {nameof(SemaphoreSlimLock)}".ToLog();
            }
#endif
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_Started, 0) != 1)
                throw new InvalidOperationException();

            m_SemaphoreSlim?.Release();
        }
    }
}