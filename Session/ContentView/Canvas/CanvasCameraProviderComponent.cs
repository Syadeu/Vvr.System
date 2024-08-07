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
// File created : 2024, 05, 29 10:05

#endregion

using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Canvas
{
    [DisallowMultipleComponent]
    public sealed class CanvasCameraProviderComponent : MonoBehaviour, ICanvasCameraProvider
    {
        [SerializeField, ChildGameObjectsOnly, Required] private Camera m_Default;
        [SerializeField, ChildGameObjectsOnly, Required] private Camera m_UI;

        public Camera Default  => m_Default;
        public Camera UICamera => m_UI;

        private void Awake()
        {
            Vvr.Provider.Provider.Static.Register<ICanvasCameraProvider>(this);
        }
        private void OnDestroy()
        {
            Vvr.Provider.Provider.Static.Unregister<ICanvasCameraProvider>(this);
        }
    }
}