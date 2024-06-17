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
// File created : 2024, 06, 17 11:06
#endregion

using System;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.UComponent.Camera
{
    [HideMonoScript, DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    internal sealed class CameraShaker : MonoBehaviour, ICameraShakeProvider
    {
        private CinemachineImpulseSource m_Source;

        private CinemachineImpulseSource Source
        {
            get
            {
                if (m_Source is null) m_Source = GetComponent<CinemachineImpulseSource>();
                return m_Source;
            }
        }

        private void Awake()
        {
            Provider.Provider.Static.Register<ICameraShakeProvider>(this);
        }
        private void OnDestroy()
        {
            Provider.Provider.Static.Unregister<ICameraShakeProvider>(this);
        }

        UniTask ICameraShakeProvider.Shake()
        {
            Source.GenerateImpulse();

            return UniTask.CompletedTask;
        }
    }
}