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
// File created : 2024, 05, 26 18:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents an attribute that indicates a wait time in a dialogue.
    /// </summary>
    [Serializable]
    [DisplayName("Wait")]
    internal sealed class DialogueWaitAttribute : IDialogueAttribute, IDialogueSkipAttribute
    {
        [SuffixLabel("seconds")]
        [SerializeField] private float m_Time = 1;

        [HideInInspector] [SerializeField] private bool m_WaitForCompletion = true;

        async UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            if (m_WaitForCompletion)
                await UniTask.WaitForSeconds(m_Time);
            else
                ctx.dialogue.RegisterTask(UniTask.WaitForSeconds(m_Time));
        }

        public override string ToString()
        {
            return $"Wait: {m_Time} seconds";
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

        public bool CanSkip            => true;
        public bool ShouldWaitForInput => false;

        public UniTask OnSkip(DialogueAttributeContext ctx)
        {
            return UniTask.CompletedTask;
        }
    }
}