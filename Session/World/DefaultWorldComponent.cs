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
// File created : 2024, 05, 30 15:05

#endregion

using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;
using Vvr.Session.AssetManagement;

namespace Vvr.Session.World
{
    [DisallowMultipleComponent]
    public class DefaultWorldComponent : MonoBehaviour
    {
        [SerializeField] private string[]
            m_EssentialDataLabels = new string[]
            {
                "GameData"
            };

        [SerializeField] private DataDownloadPopup m_DownloadPopup;

        protected virtual IEnumerator Start()
        {
            Startup().Forget();
            yield break;
        }

        protected async UniTask<DefaultWorld> Startup()
        {
            if (GameWorld.World is not null) return GameWorld.World as DefaultWorld;

            var world = await GameWorld.GetOrCreate<DefaultWorld>(Owner.Issue);

            var addressableSession
                = await world.CreateSession<AddressableSession>(new AddressableSession.SessionData(m_EssentialDataLabels));

            await addressableSession.UpdateCatalogAsync();

            long bytes = await addressableSession.GetTotalDownloadSizeAsync();

            if (bytes > 0)
            {
                if (m_DownloadPopup != null)
                {
                    UniTaskCompletionSource<bool> shouldDownload = new();
                    await m_DownloadPopup.OpenAsync(bytes, shouldDownload);

                    bool confirmation = await shouldDownload.Task;
                    if (!confirmation)
                    {
                        // TODO: exit application
                        Application.Quit();
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                        return null;
                    }

                    await addressableSession.DownloadAsync(m_DownloadPopup);

                    if (m_DownloadPopup != null)
                    {
                        while (m_DownloadPopup.DownloadSlider.value < m_DownloadPopup.DownloadSlider.maxValue - .01f)
                        {
                            await UniTask.Yield();
                        }

                        await m_DownloadPopup.CloseAsync();
                    }
                }
                else
                    await addressableSession.DownloadAsync(m_DownloadPopup);
            }

            await addressableSession.Reserve();
            return world;
        }
    }
}