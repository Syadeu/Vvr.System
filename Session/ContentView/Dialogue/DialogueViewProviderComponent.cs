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
// File created : 2024, 05, 26 10:05

#endregion

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Dialogue
{
    public abstract class DialogueViewProviderComponent
        : ContentViewProviderComponent<DialogueViewEvent>, IDialogueViewProvider
    {
#if UNITY_EDITOR
        private static DialogueViewProviderComponent s_EditorInstance;

        internal static DialogueViewProviderComponent EditorInstance
        {
            get
            {
                if (Application.isPlaying) return null;
                if (!HasEditorInstance)
                {
                    s_EditorInstance = FindAnyObjectByType<DialogueViewProviderComponent>();
                }

                return s_EditorInstance;
            }
        }
        internal static bool HasEditorInstance => s_EditorInstance != null;

        [CanBeNull]
        internal static IDialogueView EditorPreview()
        {
            if (EditorInstance == null) return null;

            EditorInstance.SetupEditorPreview();
            return EditorInstance.View;
        }

        internal static void DestroyEditorPreview()
        {
            if (s_EditorInstance == null) return;

            s_EditorInstance.SetupDestroyEditorPreview();
        }
#endif
        public sealed override Type ProviderType => VvrTypeHelper.TypeOf<IDialogueViewProvider>.Type;

        public abstract IDialogueView View { get; }
        public abstract bool IsFullyOpened { get; }

        [Conditional("UNITY_EDITOR")]
        protected virtual void SetupDestroyEditorPreview()
        {

        }
        [Conditional("UNITY_EDITOR")]
        protected virtual void SetupEditorPreview()
        {
        }
    }
}