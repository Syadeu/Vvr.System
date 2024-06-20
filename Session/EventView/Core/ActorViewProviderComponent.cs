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
// File created : 2024, 05, 18 00:05

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vvr.Controller.Actor;
using Vvr.Provider;

namespace Vvr.Session.EventView.Core
{
    public abstract class ActorViewProviderComponent : EventViewProviderComponent, IActorViewProvider
    {
        private readonly Dictionary<IEventTarget, AsyncOperationHandle<GameObject>> m_Handles = new();

        public override Type ProviderType => typeof(IActorViewProvider);

        public bool Has(IEventTarget owner) => m_Handles.ContainsKey(owner);

        public async UniTask<Transform> ResolveAsync(IEventTarget owner)
        {
            if (owner.Disposed)
                throw new ObjectDisposedException(owner.DisplayName);
            if (owner is not IActor)
                throw new InvalidOperationException("target is not actor");

            Transform result;
            if (m_Handles.TryGetValue(owner, out var t))
            {
                var obj    = await t.ToUniTask();
                result = obj.transform;
            }
            else
            {
                t                = Create(owner);
                m_Handles[owner] = t;

                result = (await t.ToUniTask()).transform;
            }

            await OnResolved(owner, result);
            return result;
        }
        public async UniTask ReleaseAsync(IEventTarget owner)
        {
            if (owner.Disposed)
                throw new ObjectDisposedException(owner.DisplayName);

            if (!m_Handles.Remove(owner, out var handle)) return;

            await OnRelease(owner, (await handle).transform);

            Addressables.Release(handle);
        }

        public abstract UniTask<GameObject> OpenAsync(ICanvasViewProvider canvasViewProvider, IAssetProvider assetProvider,
            CancellationToken                 cancellationToken);
        public abstract UniTask CloseAsync();

        public abstract UniTask ShowAsync();
        public abstract UniTask ShowAsync(IEventTarget owner);

        public abstract UniTask HideAsync();
        public abstract UniTask HideAsync(IEventTarget owner);
        public async IAsyncEnumerable<Transform> GetEnumerableAsync()
        {
            foreach (var handle in m_Handles.Values)
            {
                var tr = await handle;
                yield return tr.transform;
            }
        }

        protected abstract AsyncOperationHandle<GameObject> Create(IEventTarget     owner);

        protected virtual UniTask OnResolved(IEventTarget owner, Transform view) => UniTask.CompletedTask;
        protected virtual UniTask OnRelease(IEventTarget  owner, Transform view) => UniTask.CompletedTask;
    }
}