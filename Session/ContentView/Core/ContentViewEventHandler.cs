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

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Session.ContentView.Core
{
    class ContentViewEventHandler<TEvent> : IContentViewEventHandler<TEvent>
        where TEvent : struct, IConvertible
    {
        private readonly Dictionary<TEvent, List<uint>>                     m_Actions   = new();
        private readonly Dictionary<uint, ContentViewEventDelegate<TEvent>> m_ActionMap = new();

        private SpinLock m_SpinLock;

        private readonly CancellationTokenSource m_CancellationTokenSource = new();

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
            bool lt = false;

            try
            {
                m_SpinLock.Enter(ref lt);
                if (!m_Actions.TryGetValue(e, out var list))
                {
                    list         = new(8);
                    m_Actions[e] = list;
                }

                uint hash = CalculateHash(x);
                if (!m_ActionMap.TryAdd(hash, x))
                {
                    throw new InvalidOperationException();
                }

                m_ActionMap[hash] = x;
                list.Add(hash);
            }
            finally
            {
                if (lt)
                    m_SpinLock.Exit();
            }

            return this;
        }

        public IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x)
        {
            bool lt = false;

            try
            {
                m_SpinLock.Enter(ref lt);
                if (!m_Actions.TryGetValue(e, out var list)) return this;

                uint hash = CalculateHash(x);
                if (!m_ActionMap.Remove(hash)) return this;

                list.Remove(hash);
            }
            finally
            {
                if (lt)
                    m_SpinLock.Exit();
            }

            return this;
        }

        public async UniTask ExecuteAsync(TEvent e)
        {
            if (!m_Actions.TryGetValue(e, out var list)) return;

            int count = list.Count;
            var array = ArrayPool<UniTask>.Shared.Rent(count);

            for (int i = 0; i < count; i++)
            {
                array[i] = m_ActionMap[list[i]](e, null)
                    .AttachExternalCancellation(m_CancellationTokenSource.Token);
            }

            await UniTask.WhenAll(array);
            ArrayPool<UniTask>.Shared.Return(array, true);
        }

        public async UniTask ExecuteAsync(TEvent e, object ctx)
        {
            if (!m_Actions.TryGetValue(e, out var list)) return;

            int count = list.Count;
            var array = ArrayPool<UniTask>.Shared.Rent(count);

            for (int i = 0; i < count; i++)
            {
                array[i] = m_ActionMap[list[i]](e, ctx)
                    .AttachExternalCancellation(m_CancellationTokenSource.Token);
            }

            await UniTask.WhenAll(array);
            ArrayPool<UniTask>.Shared.Return(array, true);
        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();

            foreach (var list in m_Actions.Values)
            {
                list.Clear();
            }

            m_Actions.Clear();
            m_ActionMap.Clear();
        }
    }
}