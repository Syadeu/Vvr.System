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
// File created : 2024, 05, 30 16:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Vvr.Session.World
{
    [DisallowMultipleComponent]
    public class DataDownloadPopup : MonoBehaviour, IProgress<float>
    {
        [SerializeField, Required] private Slider m_DownloadSlider;

        public Slider DownloadSlider => m_DownloadSlider;

        public virtual UniTask OpenAsync(long downloadBytes, UniTaskCompletionSource<bool> confirmation)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask CloseAsync()
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public virtual void Report(float value)
        {
            m_DownloadSlider.value = value;
        }
    }
}