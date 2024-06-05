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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Vvr.Provider;

namespace Vvr.Session.AssetManagement
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
            /// <summary>
            /// The preloadGroups variable is an array of strings that specifies the groups of assets to be preloaded in the AssetSession class.
            /// </summary>
            public readonly string[] preloadGroups;

            public SessionData(params string[] preloadGroups)
            {
                this.preloadGroups = preloadGroups;
            }
        }

        /// <summary>
        /// Represents an immutable object that wraps a Unity Object.
        /// </summary>
        /// <remarks>
        /// This class provides a wrapper for a Unity object to ensure that it cannot be modified once created. It implements the <see cref="Vvr.Provider.IImmutableObject"/> interface.
        /// The object can be retrieved using the <see cref="Object"/> property. If an attempt is made to access the object after it has been disposed, an <see cref="ObjectDisposedException"/> will be thrown.
        /// </remarks>
        private abstract class ImmutableObject : IImmutableObject, IDisposable
        {
            private UnityEngine.Object m_Object;
            private bool               m_Disposed;

            public UnityEngine.Object Object
            {
                get
                {
                    if (m_Disposed)
                        throw new ObjectDisposedException(nameof(IImmutableObject));
                    return m_Object;
                }
            }

            protected ImmutableObject(UnityEngine.Object o)
            {
                m_Object = o;
            }

            public void Dispose()
            {
                m_Object   = null;
                m_Disposed = true;
            }
        }

        /// <summary>
        /// Represents an immutable object that wraps a Unity Object.
        /// </summary>
        /// <remarks>
        /// This class is used internally within the AssetSession class to wrap loaded Unity Objects and provide immutability guarantees.
        /// It is an abstract class that cannot be instantiated directly. Instead, concrete subclasses should be used.
        /// </remarks>
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

        private IList<IResourceLocation> m_PreloadedLocations;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            if (data.preloadGroups?.Length > 0)
            {
                await PreloadGroup(data.preloadGroups);
            }
        }

        private async UniTask PreloadGroup(params string[] preloadGroups)
        {
            Assert.IsTrue(preloadGroups.Length > 0);

            var handle =
                Addressables.LoadResourceLocationsAsync(
                    (IEnumerable)preloadGroups,
                    Addressables.MergeMode.Union,
                    typeof(UnityEngine.Object));
            await handle.ToUniTask()
                    .SuppressCancellationThrow()
                    .AttachExternalCancellation(ReserveToken)
                ;
            m_Handles.AddLast(handle);
            m_PreloadedLocations = handle.Result;

            UniTask<(bool canceled, UnityEngine.Object obj)>[] loadTasks =
                new UniTask<(bool, UnityEngine.Object)>[m_PreloadedLocations.Count];

            int i = 0;
            foreach (IResourceLocation location in m_PreloadedLocations)
            {
                var loadHandle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
                var loadTask = loadHandle.ToUniTask()
                    .SuppressCancellationThrow()
                    .AttachExternalCancellation(ReserveToken);
                loadTasks[i++] = loadTask;

                m_Handles.AddLast(loadHandle);
            }

            var results = await UniTask.WhenAll(loadTasks);

            i = 0;
            foreach (var item in results)
            {
                if (item.canceled)
                {
                    i++;
                    continue;
                }

                IResourceLocation location = m_PreloadedLocations[i++];

                var r = new ImmutableObject<UnityEngine.Object>(item.obj);

                Hash hash = new Hash(
                    FNV1a32.Calculate(location.PrimaryKey)
                    ^ FNV1a32.Calculate(location.ResourceType.AssemblyQualifiedName)
                    ^ 367
                );
                m_LoadedObjects[hash] = r;
            }
        }

        protected override UniTask OnReserve()
        {
            m_PreloadedLocations = null;

            foreach (var item in m_LoadedObjects.Values)
            {
                item.Dispose();
            }
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
            if (key is null) return null;

            if (key is AddressablePath addressablePath)
            {
                key = addressablePath.FullPath;
                Assert.IsNotNull(key);
            }

            if (key is AssetReference assetReference &&
                !assetReference.RuntimeKeyIsValid())
            {
                return null;
            }

            Type type = VvrTypeHelper.TypeOf<TObject>.Type;
            Hash hash = new Hash(
                FNV1a32.Calculate(key.ToString())
                ^ FNV1a32.Calculate(type.AssemblyQualifiedName)
                ^ 367
                );
            if (m_LoadedObjects.TryGetValue(hash, out var existingValue))
            {
                // Because preloaded assets are UnityEngine.Object type.
                if (existingValue is not ImmutableObject<TObject>)
                {
                    existingValue         = new ImmutableObject<TObject>(existingValue.Object);
                    m_LoadedObjects[hash] = existingValue;
                }
                return (ImmutableObject<TObject>)existingValue;
            }

            var handle = Addressables.LoadAssetAsync<TObject>(key);
            m_Handles.AddLast(handle);

            await handle;
            // await handle.ToUniTask()
            //     .SuppressCancellationThrow()
            //     .AttachExternalCancellation(ReserveToken);

            if (handle.OperationException is HttpRequestException http)
            {
                Debug.LogException(http);
                $"{http.Message}".ToLog();
            }

            ImmutableObject<TObject> obj = new(handle.Result);
            m_LoadedObjects[hash] = obj;

            return obj;
        }
    }
}