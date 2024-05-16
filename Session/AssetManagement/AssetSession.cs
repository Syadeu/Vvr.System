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
// File created : 2024, 05, 17 02:05

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a session for managing asset loading and caching.
    /// </summary>
    /// <remarks>
    /// This class manages the life cycle of assets. It provides asynchronous methods for loading assets into memory and retrieving
    /// previously loaded assets. When an asset is requested for the first time, it is loaded into memory and its loaded information
    /// is stored in a dictionary. Any subsequent requests for the same asset will then retrieve the loaded asset from the dictionary,
    /// rather than loading it anew. To prevent memory leakage, it also provides functionality to clear the loaded assets when a session
    /// is reserved. In essence, this class ensures that each asset is loaded only once per session, and memory is cleared when it is no longer needed.
    /// </remarks>
    [UsedImplicitly]
    public class AssetSession : ChildSession<AssetSession.SessionData>,
        IAssetProvider
    {
        public struct SessionData : ISessionData
        {
        }

        private abstract class ImmutableObject : IImmutableObject
        {
            public UnityEngine.Object Object { get; }

            protected ImmutableObject(UnityEngine.Object o)
            {
                Object = o;
            }
        }
        private sealed class ImmutableObject<T> : ImmutableObject, IImmutableObject<T>
            where T : UnityEngine.Object
        {
            public new T Object => (T)base.Object;

            public ImmutableObject(UnityEngine.Object o) : base(o)
            {
            }
        }

        public override string DisplayName => nameof(AssetSession);

        private readonly LinkedList<AsyncOperationHandle>  m_Handles       = new();
        private readonly Dictionary<Hash, ImmutableObject> m_LoadedObjects = new();

        protected override UniTask OnReserve()
        {
            m_LoadedObjects.Clear();
            foreach (var item in m_Handles)
            {
                Addressables.Release(item);
            }
            return base.OnReserve();
        }

        public async UniTask<IImmutableObject<TObject>> LoadAsync<TObject>(object key)
            where TObject : UnityEngine.Object
        {
            Hash hash = new Hash(key.ToString());
            if (m_LoadedObjects.TryGetValue(hash, out var existingValue))
                return (ImmutableObject<TObject>)existingValue;

            AsyncOperationHandle<TObject> handle = Addressables.LoadAssetAsync<TObject>(key);
            m_Handles.AddLast(handle);

            await handle.ToUniTask()
                .SuppressCancellationThrow()
                .AttachExternalCancellation(ReserveToken);

            ImmutableObject<TObject> obj = new(handle.Result);
            m_LoadedObjects[hash] = obj;

            return obj;
        }
    }
}