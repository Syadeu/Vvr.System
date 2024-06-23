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
// File created : 2024, 05, 28 12:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vvr.Provider;

namespace Vvr.Session.AssetManagement
{
    /// <summary>
    /// Represents a child session for addressable assets management.
    /// </summary>
    /// <remarks>
    /// This session provides all necessary network addressable methods
    /// includes download content catalogs from remote.
    /// </remarks>
    [UsedImplicitly]
    public sealed class AddressableSession : ChildSession<AddressableSession.SessionData>,
        IAddressableDownloadProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly string[] labels;

            public SessionData(params string[] label)
            {
                labels = label;
            }
        }

        public override string DisplayName => nameof(AddressableSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);
            await Addressables.InitializeAsync();
        }

        /**
         * Do not use UniTask if operation is server request.
         * It will not work
         */

        // https://forum.unity.com/threads/unable-to-load-dependent-bundle-from-location.1336874/

        public async UniTask UpdateCatalogAsync()
        {
            var updateCheckOperation    = Addressables.CheckForCatalogUpdates(false);
            var requireUpdatedList = await updateCheckOperation.Task;
            if (updateCheckOperation.Status == AsyncOperationStatus.Failed)
            {
                throw new InvalidOperationException("Update catalog check has been failed");
            }
            Addressables.Release(updateCheckOperation);

            if (requireUpdatedList.Count > 0)
            {
                var updateOperation = Addressables.UpdateCatalogs(requireUpdatedList, false);
                await updateOperation.Task;
                if (updateOperation.Status == AsyncOperationStatus.Failed)
                {
                    throw new InvalidOperationException("Update catalog has been failed");
                }
                Addressables.Release(updateOperation);
            }
        }

        public async UniTask<long> GetTotalDownloadSizeAsync()
        {
            long size = 0;
            for (int i = 0; i < Data.labels.Length; i++)
            {
                var downloadSizeOperation = Addressables.GetDownloadSizeAsync(Data.labels[i]);
                size += await downloadSizeOperation.Task;

                Addressables.Release(downloadSizeOperation);
            }

            return size;
        }

        public async UniTask DownloadAsync(IProgress<float> progress)
        {
            float vs = 1f / Data.labels.Length;

            float current = 0;
            for (int i = 0; i < Data.labels.Length; i++)
            {
                var operation = Addressables.DownloadDependenciesAsync(Data.labels[i]);
                while (!operation.IsDone &&
                       operation.Status != AsyncOperationStatus.Failed)
                {
                    progress?.Report(current + operation.PercentComplete * vs);

                    await UniTask.Yield();
                }

                await operation.Task;
                if (operation.Status == AsyncOperationStatus.Failed)
                {
                    $"Download dependencies({Data.labels[i]}) has been failed".ToLogError();
                }

                Addressables.Release(operation);
                current += vs;
            }

            progress?.Report(1);
        }
    }

    [PublicAPI, LocalProvider]
    public interface IAddressableDownloadProvider : IProvider
    {
        UniTask       UpdateCatalogAsync();
        UniTask<long> GetTotalDownloadSizeAsync();
        UniTask       DownloadAsync([CanBeNull] IProgress<float> progress);
    }
}