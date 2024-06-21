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
// File created : 2024, 06, 21 21:06

#endregion

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vvr.Provider
{
    [PublicAPI, LocalProvider]
    public interface IEffectViewProvider : IProvider
    {
        UniTask SpawnAsync(
            [NotNull] object            key,
            Vector3           position,
            CancellationToken cancellationToken);
        UniTask SpawnAsync(
            [NotNull] object            key,
            Vector3           position, Quaternion rotation, [CanBeNull] Transform parent,
            CancellationToken cancellationToken);
    }

    [PublicAPI]
    public struct EffectEmitter
    {
        private readonly IEffectViewProvider m_ViewProvider;

        private readonly object m_Key;

        private readonly AutoResetUniTaskCompletionSource m_Source;

        public event Action OnSpawn;

        public UniTask Task => m_Source.Task;

        public EffectEmitter(IEffectViewProvider p, [CanBeNull] object key)
        {
            Assert.IsNotNull(p, "p != null");

            m_ViewProvider = p;
            m_Key          = key;
            m_Source = AutoResetUniTaskCompletionSource.Create();

            OnSpawn = null;
        }

        public UniTask SpawnAsync(
            Vector3           position,
            CancellationToken cancellationToken)
        {
            if (m_ViewProvider is null) return UniTask.CompletedTask;

            OnSpawn?.Invoke();
            m_Source.TrySetResult();

            if (m_Key is null) return UniTask.CompletedTask;
            return m_ViewProvider.SpawnAsync(m_Key, position, cancellationToken);
        }
        public UniTask SpawnAsync(Vector3 position, Quaternion rotation, Transform parent,
            CancellationToken        cancellationToken)
        {
            if (m_ViewProvider is null) return UniTask.CompletedTask;

            OnSpawn?.Invoke();
            m_Source.TrySetResult();

            if (m_Key is null) return UniTask.CompletedTask;
            return m_ViewProvider.SpawnAsync(m_Key, position, rotation, parent, cancellationToken);
        }
    }
}