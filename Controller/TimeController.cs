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
// File created : 2024, 05, 07 01:05

#endregion

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Crypto;
using Vvr.Model;

namespace Vvr.Controller
{
    [PublicAPI]
    public struct TimeController
    {
        sealed class Entry : IDisposable
        {
            private readonly  ITimeUpdate             m_Updater;
            private readonly CancellationTokenSource m_CancellationToken;
            private readonly CancellationTokenSource m_LinkedToken;

            private          bool          m_BackgroundTask;
            private readonly Func<UniTask> m_UpdateFunc, m_EndUpdateFunc;

            private float m_CurrentTime, m_Delta;

            public CancellationToken CancellationToken => m_LinkedToken.Token;

            public Entry(ITimeUpdate t, bool background, CancellationToken s)
            {
                m_Updater           = t;
                m_BackgroundTask    = background;
                m_CancellationToken = new CancellationTokenSource();

                if (s.CanBeCanceled)
                    m_LinkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                        m_CancellationToken.Token,
                        s);
                else
                    m_LinkedToken = m_CancellationToken;

                m_UpdateFunc    = Internal_OnUpdateTimeAsync;
                m_EndUpdateFunc = m_Updater.OnEndUpdateTime;
            }

            public UniTask OnUpdateTimeAsync(float currentTime, float delta)
            {
                m_CurrentTime = currentTime;
                m_Delta       = delta;

                if (m_BackgroundTask)
                    return UniTask
                        .RunOnThreadPool(m_UpdateFunc, true, CancellationToken);
                return UniTask.Create(m_UpdateFunc)
                    .AttachExternalCancellation(CancellationToken);
            }

            public UniTask OnEndUpdateTimeAsync()
            {
                if (m_BackgroundTask)
                    return UniTask
                        .RunOnThreadPool(m_EndUpdateFunc, true, CancellationToken);

                return UniTask.Create(m_EndUpdateFunc)
                    .AttachExternalCancellation(CancellationToken);
            }

            private UniTask Internal_OnUpdateTimeAsync()
            {
                return m_Updater.OnUpdateTime(m_CurrentTime, m_Delta);
            }

            public void Dispose()
            {
                m_LinkedToken.Cancel();

                m_CancellationToken?.Dispose();
                m_LinkedToken?.Dispose();
            }
        }

        private static readonly Dictionary<ITimeUpdate, Entry> s_TimeUpdaters = new();

        private static int   s_IsUpdating;
        private static float s_CurrentTime;

        private static readonly SemaphoreSlim           s_Slim                    = new(1, 1);
        private static          CancellationTokenSource s_CancellationTokenSource = new();

        public static CryptoFloat CurrentTime => CryptoFloat.Raw(s_CurrentTime);
        public static bool        IsUpdating  => s_IsUpdating == 1;

        [ThreadSafe]
        public static void Register(ITimeUpdate t, bool background = false, CancellationToken externalToken = default)
        {
            using var l = new SemaphoreSlimLock(s_Slim);
            l.Wait(TimeSpan.FromSeconds(1));

            if (s_TimeUpdaters.TryGetValue(t, out var e))
                throw new InvalidOperationException();

            s_TimeUpdaters[t] = new(t, background, externalToken);
        }
        [ThreadSafe]
        public static void Unregister(ITimeUpdate t)
        {
            using var l = new SemaphoreSlimLock(s_Slim);
            l.Wait(TimeSpan.FromSeconds(1));

            if (!s_TimeUpdaters.Remove(t, out var v)) return;

            v.Dispose();
        }

        [ThreadSafe]
        public static void ResetTime()
        {
            using var l = new SemaphoreSlimLock(s_Slim);
            l.Wait(TimeSpan.FromSeconds(1));

            s_CancellationTokenSource.Cancel();

            if (IsUpdating)
                throw new InvalidOperationException();

            s_CancellationTokenSource = new();
            Interlocked.Exchange(ref s_CurrentTime, 0);
            Array.Clear(s_CachedArray, 0, s_CachedArray.Length);
        }

        public static async UniTask WaitForUpdateCompleted()
        {
            while (IsUpdating)
            {
                await UniTask.Yield();
            }
        }

        private static Entry[] s_CachedArray = new Entry[8];

        [ThreadSafe]
        public static async UniTask Next(float time, CancellationToken cancellationToken = default)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await InternalNext(time, s_CancellationTokenSource.Token);
            }
            else
            {
                if (cancellationToken.IsCancellationRequested) return;

                using var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    s_CancellationTokenSource.Token, cancellationToken);

                await InternalNext(time, cancelTokenSource.Token);
            }

            Array.Clear(s_CachedArray, 0, s_CachedArray.Length);
        }

        [ThreadSafe]
        private static async UniTask InternalNext(float delta, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            using var sps = new SemaphoreSlimLock(s_Slim);
            await sps.WaitAsync(cancellationToken);

            CryptoFloat f           = CurrentTime + delta;
            float       currentTime = f;
            Interlocked.Exchange(ref s_IsUpdating, 1);
            Interlocked.Exchange(ref s_CurrentTime, f.RawValue);

            // This because time updater container can be changed
            // while updating their state.
            int count = s_TimeUpdaters.Count;
            if (s_CachedArray.Length <= count)
            {
                s_CachedArray = new Entry[s_CachedArray.Length * 2];
            }

            UpdateCachedArray(ref s_CachedArray, s_TimeUpdaters.Values);

            using var taskArray = TempArray<UniTask>.Shared(count, true);

            await ExecuteOnUpdateTime(
                        taskArray.Value, currentTime, delta, s_CachedArray, count)
                    .SuppressCancellationThrow()
                    .AttachExternalCancellation(cancellationToken)
                    ;

            count = UpdateCachedArray(ref s_CachedArray, s_TimeUpdaters.Values);

            await ExecuteOnEndUpdateTime(taskArray.Value, s_CachedArray, count)
                        .SuppressCancellationThrow()
                        .AttachExternalCancellation(cancellationToken)
                    ;

            Interlocked.Exchange(ref s_IsUpdating, 0);
        }

        private static int UpdateCachedArray(
            ref Entry[] array, IEnumerable<Entry> entries)
        {
            int i = 0;
            foreach (var item in entries)
            {
                array[i++] = item;
            }

            return i;
        }

        private static UniTask ExecuteOnUpdateTime(
            UniTask[] taskArray,
            float currentTime, float delta,
            Entry[] entries, int count)
        {
            int i = 0;
            for (; i < count; i++)
            {
                var t = s_CachedArray[i];
                taskArray[i] = t.OnUpdateTimeAsync(currentTime, delta);
            }

            for (; i < taskArray.Length; i++)
            {
                taskArray[i] = UniTask.CompletedTask;
            }

            return UniTask.WhenAll(taskArray);
        }

        private static UniTask ExecuteOnEndUpdateTime(
            UniTask[] taskArray,
            Entry[] entries, int count)
        {
            int i = 0;
            for (; i < count; i++)
            {
                var t = s_CachedArray[i];
                taskArray[i] = t.OnEndUpdateTimeAsync();
            }

            for (; i < taskArray.Length; i++)
            {
                taskArray[i] = UniTask.CompletedTask;
            }

            return UniTask.WhenAll(taskArray);
        }
    }

    public interface ITimeUpdate
    {
        UniTask OnUpdateTime(float currentTime, float deltaTime);
        UniTask OnEndUpdateTime();
    }
}