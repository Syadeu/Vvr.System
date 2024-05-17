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
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vvr.Provider
{
    public abstract class EventViewProviderComponent : MonoBehaviour, IEventViewProvider
    {
        private readonly Dictionary<IEventTarget, AsyncOperationHandle<GameObject>> m_Handles = new();

        bool IEventViewProvider.Has(IEventTarget owner) => m_Handles.ContainsKey(owner);

        async UniTask<Transform> IEventViewProvider.Resolve(IEventTarget owner)
        {
            if (owner.Disposed)
                throw new ObjectDisposedException(owner.DisplayName);

            Transform result;
            if (m_Handles.TryGetValue(owner, out var t))
            {
                var obj    = await t.ToUniTask();
                result = obj.transform;
            }
            else
            {
                if (!CanResolve(owner))
                    throw new Exception($"Cant resolve target {owner.DisplayName} for this provider {GetType().FullName}");

                t                = await Create(owner);
                m_Handles[owner] = t;

                result = (await t.ToUniTask()).transform;
            }

            await OnResolved(owner, result);
            return result;
        }

        async UniTask IEventViewProvider.Release(IEventTarget owner)
        {
            if (owner.Disposed)
                throw new ObjectDisposedException(owner.DisplayName);

            if (!m_Handles.Remove(owner, out var handle)) return;

            await OnRelease(owner, (await handle).transform);

            Addressables.Release(handle);
        }

        protected virtual bool CanResolve(IEventTarget owner) => true;

        protected abstract UniTask<AsyncOperationHandle<GameObject>> Create(IEventTarget     owner);

        protected virtual UniTask OnResolved(IEventTarget owner, Transform view) => UniTask.CompletedTask;
        protected virtual UniTask OnRelease(IEventTarget  owner, Transform view) => UniTask.CompletedTask;
    }
}