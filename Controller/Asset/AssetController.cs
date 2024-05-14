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
// File created : 2024, 05, 14 01:05
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Asset
{
    public class AssetController<TAssetType> : IAsset, IDisposable
        where TAssetType : struct, IConvertible
    {
        private readonly IEventTarget            m_Owner;
        private CancellationTokenSource m_CancellationTokenSource;

        private readonly Dictionary<TAssetType, AsyncLazy<UnityEngine.Object>> m_Assets;

        private readonly LinkedList<AsyncOperationHandle> m_Handles = new();

        public AsyncLazy<UnityEngine.Object> this[TAssetType t] => m_Assets[t];
        AsyncLazy<UnityEngine.Object> IAsset.this[AssetType              t] => m_Assets[(TAssetType)(object)(short)t];

        public AssetController(IEventTarget owner)
        {
            m_Owner                   = owner;
            m_CancellationTokenSource = new();

            m_Assets = new();
        }

        public void Connect<TLoadProvider>(IReadOnlyDictionary<TAssetType, AddressablePath> t)
            where TLoadProvider : IAssetLoadTaskProvider<TAssetType>
        {
            m_CancellationTokenSource ??= new();
            foreach (var item in t)
            {
                m_Assets[item.Key] = UniTask.Lazy(
                    async () =>
                    {
                        var loadTaskProvider = Activator.CreateInstance<TLoadProvider>();

                        var handle = loadTaskProvider.Resolve(item.Key, item.Value.FullPath);
                        while (!handle.IsDone)
                        {
                            await UniTask.Yield();
                            if (m_CancellationTokenSource.IsCancellationRequested)
                            {
                                return null;
                            }
                        }

                        m_Handles.AddLast(handle);
                        return (UnityEngine.Object)handle.Result;
                    }
                );
            }
        }

        public void Clear()
        {
            m_CancellationTokenSource.Cancel();

            foreach (var handle in m_Handles)
            {
                Addressables.Release(handle);
            }

            m_Handles.Clear();
            m_Assets.Clear();

            m_CancellationTokenSource.Dispose();
            m_CancellationTokenSource = null;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}