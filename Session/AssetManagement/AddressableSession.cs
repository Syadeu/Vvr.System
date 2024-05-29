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
    [ParentSession(typeof(GameDataSession))]
    internal sealed class AddressableSession : ChildSession<AddressableSession.SessionData>
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

        /**
         * Do not use UniTask if operation is server request.
         * It will not work
         */

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            // https://forum.unity.com/threads/unable-to-load-dependent-bundle-from-location.1336874/

            await UpdateCatalogAsync();

            long downloadSize = await GetTotalDownloadSizeAsync();
            $"Data download: {downloadSize}bytes, {downloadSize / 1024 / 1024}mb".ToLog();

            await DownloadAsync();
        }

        private async UniTask UpdateCatalogAsync()
        {
            var updateCheckOperation    = Addressables.CheckForCatalogUpdates(true);
            var requireUpdatedList = await updateCheckOperation.Task;
            if (updateCheckOperation.Status == AsyncOperationStatus.Failed)
            {
                throw new InvalidOperationException("Update catalog check has been failed");
            }

            if (requireUpdatedList.Count > 0)
            {
                var updateOperation = Addressables.UpdateCatalogs(requireUpdatedList, true);
                await updateOperation.Task;
                if (updateOperation.Status == AsyncOperationStatus.Failed)
                {
                    throw new InvalidOperationException("Update catalog has been failed");
                }
            }
        }

        private async UniTask<long> GetTotalDownloadSizeAsync()
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

        private async UniTask DownloadAsync()
        {
            // List<Task> ops = new();
            for (int i = 0; i < Data.labels.Length; i++)
            {
                var operation = Addressables.DownloadDependenciesAsync(Data.labels[i]);
                // ops.Add(operation.Task);
                await operation.Task;
                if (operation.Status == AsyncOperationStatus.Failed)
                {
                    $"Download dependencies({Data.labels[i]}) has been failed".ToLogError();
                }

                Addressables.Release(operation);
            }

            // await Task.WhenAll(ops);
        }
    }
}