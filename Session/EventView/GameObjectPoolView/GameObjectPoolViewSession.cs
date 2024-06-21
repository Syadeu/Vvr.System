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
// File created : 2024, 06, 21 20:06
#endregion

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.EventView.Core;

namespace Vvr.Session.EventView.GameObjectPoolView
{
    [UsedImplicitly, PublicAPI]
    public class GameObjectPoolViewSession : ParentSession<GameObjectPoolViewSession.SessionData>,
        IGameObjectPoolViewProvider
    {
        public struct SessionData : ISessionData
        {
            public string rootObjectName;
        }

        [HideMonoScript]
        [DisallowMultipleComponent]
        class ObjectPoolItem : MonoBehaviour
        {
            [ShowInInspector, ReadOnly]
            public int Key { get; set; }
        }

        public override string DisplayName => nameof(GameObjectPoolViewSession);

        private IAssetProvider m_AssetProvider;

        private readonly Stack<GameObject>                  m_CreatedInstances = new();
        private readonly Dictionary<int, Stack<GameObject>> m_Pool             = new();

        private          Transform                 m_Root;
        private readonly Dictionary<int, Transform> m_PoolParent = new();

        public Transform RootTransform => m_Root;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSession<AssetSession>(default);

            await UniTask.Create(SetupRootObject);
        }

        protected override UniTask OnReserve()
        {
            while (m_CreatedInstances.TryPop(out var ins))
            {
                UnityEngine.Object.Destroy(ins);
            }

            foreach (var pool in m_Pool.Values)
            {
                while (pool.TryPop(out var ins))
                {
                    UnityEngine.Object.Destroy(ins);
                }
            }
            m_Pool.Clear();

            return base.OnReserve();
        }

        private async UniTask SetupRootObject()
        {
            GameObject rootObj = new GameObject(
                Data.rootObjectName.IsNullOrEmpty() ? DisplayName : Data.rootObjectName
            );
            rootObj.hideFlags = HideFlags.NotEditable;
            m_Root            = rootObj.transform;
        }

        public async UniTask<GameObjectPoolScope> Scope(object key, CancellationToken cancellationToken)
        {
            return await GameObjectPoolScope.Create(this, key, cancellationToken);
        }

        public async UniTask<GameObject> CreateInstanceAsync(object key, CancellationToken cancellationToken)
        {
            Assert.IsNotNull(key, "key != null");

            var asset = await m_AssetProvider.LoadAsync<GameObject>(key)
                    .AttachExternalCancellation(cancellationToken)
                    .SuppressCancellationThrow()
                ;
            if (asset.IsCanceled)
                return null;

            int hash = asset.GetHashCode();
            if (m_Pool.TryGetValue(hash, out var pool) &&
                pool.TryPop(out var ins))
            {
                return ins;
            }

            ins = asset.Result.CreateInstance();
            var poolItem = ins.AddComponent<ObjectPoolItem>();
            poolItem.Key = hash;

            if (!m_PoolParent.TryGetValue(hash, out var parent))
            {
                GameObject obj = new GameObject(asset.Result.Object.name);
                obj.hideFlags = HideFlags.NotEditable;
                parent        = obj.transform;
                parent.SetParent(m_Root);

                m_PoolParent[hash] = parent;
            }
            ins.transform.SetParent(parent);

            m_CreatedInstances.Push(ins);

            return ins;
        }

        public void ReserveInstance(GameObject obj)
        {
            Assert.IsNotNull(obj, "obj != null");

            var poolItem = obj.GetComponent<ObjectPoolItem>();
            Assert.IsNotNull(poolItem, "poolItem != null");

            if (!m_Pool.TryGetValue(poolItem.Key, out var pool))
            {
                pool                 = new();
                m_Pool[poolItem.Key] = pool;
            }

            obj.transform.SetParent(m_PoolParent[poolItem.Key], false);

            pool.Push(obj);
        }
    }
}