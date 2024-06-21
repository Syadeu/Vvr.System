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
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Model;
using Object = UnityEngine.Object;

namespace Vvr.Session.EventView.Core
{
    [PublicAPI]
    public struct GameObjectPoolScope : IDisposable
    {
        class InstanceHandle : IImmutableObject<GameObject>
        {
            Object IImmutableObject.Object => Object;

            public GameObject Object { get; set; }
        }

        private static readonly Stack<InstanceHandle> s_Pool = new();

        public static async UniTask<GameObjectPoolScope> Create(
            [NotNull] IGameObjectPoolViewProvider p, [NotNull] object key,
            CancellationToken cancellationToken)
        {
            var ins = await p.CreateInstanceAsync(key, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                Assert.IsNull(ins);
                return default;
            }

            if (!s_Pool.TryPop(out var handle))
            {
                handle = new();
            }

            handle.Object = ins;
            return new GameObjectPoolScope(p, handle);
        }

        private readonly IGameObjectPoolViewProvider m_Provider;
        private readonly InstanceHandle              m_Instance;

        public GameObject Object
        {
            get
            {
                if (m_Instance?.Object is null)
                    throw new ObjectDisposedException(nameof(GameObjectPoolScope));
                return m_Instance.Object;
            }
        }

        private GameObjectPoolScope([NotNull] IGameObjectPoolViewProvider p, [NotNull] InstanceHandle ins)
        {
            Assert.IsNotNull(p);
            Assert.IsNotNull(ins);

            m_Provider = p;
            m_Instance = ins;
        }

        public void Dispose()
        {
            m_Provider.ReserveInstance(m_Instance.Object);

            m_Instance.Object = null;
            s_Pool.Push(m_Instance);
        }
    }
}