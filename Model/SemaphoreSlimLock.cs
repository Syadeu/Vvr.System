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

        private int m_CurrentThreadId;
        private int m_Started;

        public SemaphoreSlimLock(SemaphoreSlim s)
        {
            m_SemaphoreSlim   = s;
            m_CurrentThreadId = 0;
            m_Started         = 0;
        }

        public UniTask WaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

            if (!isCurrentWriteThread)
                return m_SemaphoreSlim.WaitAsync(cancellationToken).AsUniTask();

            return UniTask.CompletedTask;
        }
        public UniTask WaitAsync(TimeSpan timeout)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

            if (!isCurrentWriteThread)
                return m_SemaphoreSlim.WaitAsync()
                    .AsUniTask()
                    .TimeoutWithoutException(timeout);

            return UniTask.CompletedTask;
        }

        public void Wait(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

#if THREAD_DEBUG
            if (m_SemaphoreSlim.CurrentCount == 0 &&
                !isCurrentWriteThread)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }
#endif

            if (!isCurrentWriteThread)
                m_SemaphoreSlim.Wait(cancellationToken);
        }

        public void Wait(TimeSpan timeout)
        {
            if (Interlocked.Exchange(ref m_Started, 1) == 1)
                throw new InvalidOperationException();

            int threadId = Thread.CurrentThread.ManagedThreadId;
            bool isCurrentWriteThread
                = Interlocked.Exchange(ref m_CurrentThreadId, threadId) == threadId;

#if THREAD_DEBUG
            if (m_SemaphoreSlim.CurrentCount == 0 &&
                !isCurrentWriteThread)
            {
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                    throw new InvalidOperationException(
                        "Another thread currently writing but main thread trying to write." +
                        "This is not allowed main thread should not be awaited.");
            }
#endif

            if (!isCurrentWriteThread)
                if (!m_SemaphoreSlim.Wait(timeout))
                    throw new TimeoutException();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_Started, 0) != 1)
                throw new InvalidOperationException();

            m_SemaphoreSlim?.Release();
        }
    }
}