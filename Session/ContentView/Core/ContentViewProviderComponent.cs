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
// File created : 2024, 06, 02 22:06

#endregion

using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    public abstract class ContentViewProviderComponent : MonoBehaviour, IContentViewProvider
    {
        public abstract System.Type EventType { get; }
        public abstract System.Type ProviderType { get; }

        public abstract void    Reserve();

        public abstract UniTask OpenAsync(
            ICanvasViewProvider canvasProvider,
            IAssetProvider      assetProvider,
            object              ctx);

        public abstract UniTask CloseAsync(object ctx);
    }
}