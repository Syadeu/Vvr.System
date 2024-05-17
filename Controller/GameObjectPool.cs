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
// File created : 2024, 05, 07 22:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Vvr.Controller
{
    /// <summary>
    /// Represents a pool of GameObject instances that can be spawned and reserved.
    /// </summary>
    [Obsolete]
    internal sealed class GameObjectPool : MonoBehaviour
    {
        [DisallowMultipleComponent]
        class ParticleCallbackReceiver : MonoBehaviour, IEffectObject
        {
            public GameObjectPool Pool     { get; set; }
            public GameObject     Root     { get; set; }
            public bool           Reserved { get; set; }

            private void OnParticleSystemStopped()
            {
                Pool.Reserve(Root);
                Reserved = true;
            }
        }

        private static readonly Dictionary<Hash, GameObjectPool> s_ObjectPools = new();

        /// <summary>
        /// Retrieves or creates a GameObjectPool instance for the given AddressablePath.
        /// <param name="path">The AddressablePath representing the path to the Unity Addressable Asset.</param>
        /// <returns>The GameObjectPool instance associated with the given AddressablePath.</returns>
        public static GameObjectPool Get(AddressablePath path)
        {
            Hash h = new(path.FullPath);
            if (!s_ObjectPools.TryGetValue(h, out var pool))
            {
                GameObject obj = new GameObject(path.FullPath);
                pool             = obj.AddComponent<GameObjectPool>();

                pool.Initialize(h, path);

                s_ObjectPools[h] = pool;
            }

            return pool;
        }

        private Hash            m_Hash;
        private AddressablePath m_Path;

        private readonly List<AsyncOperationHandle> m_OperationHandles = new();
        private readonly Stack<GameObject>          m_Pool             = new();

        private void Initialize(Hash h, AddressablePath path)
        {
            m_Hash = h;
            m_Path = path;
        }

        private AsyncOperationHandle<GameObject> CreateInstance(
            Vector3   position, Quaternion rotation, Transform parent = null)
        {
            AsyncOperationHandle<GameObject> handle =
                Addressables.InstantiateAsync(
                    m_Path.FullPath,
                    new InstantiationParameters(
                        position,
                        rotation,
                        parent));
            m_OperationHandles.Add(handle);

            return handle;
        }

        public async UniTask<IEffectObject> SpawnEffect(
            Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var obj            = await Spawn(position, rotation, parent);
            var particleSystem = obj.GetComponentInChildren<ParticleSystem>();
            var main           = particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            particleSystem.Play();

            var callback = particleSystem.gameObject.GetOrAddComponent<ParticleCallbackReceiver>();
            callback.Reserved = false;
            callback.Root     = obj;
            callback.Pool     = this;


            return callback;
        }
        public async UniTask<GameObject> Spawn(
            Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (parent is null) parent = transform;

            if (m_Pool.TryPop(out var result))
            {
                var tr  = result.transform;
                tr.SetParent(parent, false);
                tr.position = position;
                tr.rotation = rotation;

                result.SetActive(true);
                return result;
            }

            var handle = CreateInstance(position, rotation, parent);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            var obj = handle.Result;
            obj.SetActive(true);
            return obj;
        }

        public void Reserve(GameObject ins)
        {
            ins.SetActive(false);
            ins.transform.SetParent(transform, false);
            m_Pool.Push(ins);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < m_OperationHandles.Count; i++)
            {
                var e = m_OperationHandles[i];
                Addressables.Release(e);
            }
            m_OperationHandles.Clear();
            m_Pool.Clear();

            s_ObjectPools.Remove(m_Hash);
        }
    }

    public interface IEffectObject
    {
        bool Reserved { get; }
    }
}