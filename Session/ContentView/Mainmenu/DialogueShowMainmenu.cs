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
// File created : 2024, 05, 30 18:05

#endregion

using System.ComponentModel;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.Mainmenu
{
    [UsedImplicitly]
    [DisplayName("Show Mainmenu")]
    class DialogueShowMainmenu : IDialogueAttribute
    {
        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = false;

        async UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            var task = ctx.eventHandlerProvider.Mainmenu.ExecuteAsync(MainmenuViewEvent.Show);

            if (m_WaitForCompletion)
                await task;
            else
                ctx.dialogue.RegisterTask(task);
        }

#if UNITY_EDITOR
        [ShowIf(nameof(m_WaitForCompletion))]
        [VerticalGroup("0")]
        [Button(ButtonSizes.Medium, DirtyOnClick = true), GUIColor(0, 1, 0)]
        private void WaitForCompletion() => m_WaitForCompletion = false;

        [HideIf(nameof(m_WaitForCompletion))]
        [VerticalGroup("0")]
        [Button(ButtonSizes.Medium, DirtyOnClick = true), GUIColor(1, .2f, 0)]
        private void DontWaitForCompletion() => m_WaitForCompletion = true;
#endif

        public override string ToString() => "Show Mainmenu";
    }
}