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

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Provider;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.WorldBackground
{
    public abstract class WorldBackgroundViewProvider : MonoBehaviour, IWorldBackgroundViewProvider
    {
#if UNITY_EDITOR
        private static WorldBackgroundViewProvider s_EditorInstance;

        internal static WorldBackgroundViewProvider EditorInstance
        {
            get
            {
                if (s_EditorInstance is null)
                {
                    s_EditorInstance = FindAnyObjectByType<WorldBackgroundViewProvider>();
                }

                return s_EditorInstance;
            }
        }

        [CanBeNull]
        internal static IWorldBackgroundView EditorPreview()
        {
            if (EditorInstance == null)
            {
                "instance not found".ToLogError();
                return null;
            }

            return EditorInstance.GetEditorView();
        }
        [CanBeNull]
        protected virtual IWorldBackgroundView GetEditorView()
        {
            throw new NotImplementedException();
        }
#endif

        public abstract void Initialize(IContentViewEventHandler<WorldBackgroundViewEvent> eventHandler);
        public abstract void Reserve();

        public abstract UniTask OpenAsync(ICanvasViewProvider canvasProvider, IAssetProvider assetProvider, object ctx);
        public abstract UniTask CloseAsync(object             ctx);

        public abstract IWorldBackgroundView GetView(object ctx);
    }
}