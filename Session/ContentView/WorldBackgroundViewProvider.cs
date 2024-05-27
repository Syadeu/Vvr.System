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
// File created : 2024, 05, 27 10:05

#endregion

using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;
using Vvr.Session.ContentView.WorldBackground;

namespace Vvr.Session.ContentView
{
    public abstract class WorldBackgroundViewProvider : MonoBehaviour, IWorldBackgroundViewProvider
    {
        public abstract void Initialize(IContentViewEventHandler<WorldBackgroundViewEvent> eventHandler);
        public abstract void Reserve();

        public abstract UniTask OpenAsync(IAssetProvider assetProvider, object ctx);
        public abstract UniTask CloseAsync(object        ctx);

        public abstract IWorldBackgroundView GetView(object ctx);
    }
}