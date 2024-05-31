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
// File created : 2024, 05, 21 09:05

#endregion

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.Dialogue
{
    /// <summary>
    /// Represents a class that holds data for a dialogue in a content view session.
    /// </summary>
    /// <remarks>
    /// This class is used to store information about a dialogue in a content view session.
    /// It implements the <see cref="IDialogueData"/> interface.
    /// </remarks>
    [HideMonoScript]
    [CreateAssetMenu(menuName = "Vvr/Create DialogueData", fileName = "DialogueData", order = 0)]
    public class DialogueData : ScriptableObject, IDialogueData
    {
        [SerializeField] private int    m_Index;

        [Space]
        [ListDrawerSettings(
            AlwaysAddDefaultValue = true,
            ShowPaging = false)]
        [SerializeField] private RawDialogueAttribute[] m_Attributes;

        [Space] [SerializeField]
        private DialogueData m_NextDialogue;

        private IDialogueAttribute[] m_ResolvedAttributes;

        public string Id => name;
        public int Index => m_Index;

        public IReadOnlyList<IDialogueAttribute> Attributes
        {
            get
            {
                m_ResolvedAttributes ??= m_Attributes.Select(x => x.Value).ToArray();
                return m_ResolvedAttributes;
            }
        }
        public IDialogueData NextDialogue => m_NextDialogue;

        public bool IsEnabled(int index) => m_Attributes[index].Enabled;

#if UNITY_EDITOR
        private Color NormalColor => new Color(0.15f, 0.47f, 0.74f);
        private Color NoPreviewColor => new Color(1, 0.57f, 0.34f);

        private Color GetPreviewProgressColor(int value)
        {
            if (value - 1           < 0) return NoPreviewColor;
            if (m_Attributes.Length <= value - 1) return GetPreviewProgressColor(m_Attributes.Length - 1);

            var target = m_Attributes[value - 1];
            if (!target.Enabled) return NoPreviewColor;

            bool hasPreview = target.Value is IDialoguePreviewAttribute;
            return hasPreview ? NormalColor : NoPreviewColor;
        }

        private void OnPreviewProgressValueChanged(int value)
        {
            IDialogueView ins;
            if (value - 1 < 0)
            {
                ins = DialogueViewProviderComponent.EditorPreview();
                if (ins is null) return;

                ins.LeftPortrait.Clear();
                ins.RightPortrait.Clear();
                ins.Text.Clear();

                return;
            }

            if (m_Attributes.Length <= value - 1) return;

            var target = m_Attributes[value - 1];
            if (target.Value is not IDialoguePreviewAttribute previewAttribute) return;

            ins = DialogueViewProviderComponent.EditorPreview();
            if (ins is null) return;

            previewAttribute.Preview(ins);
        }

        [ShowInInspector, HideLabel]
        [TitleGroup("Preview")]
        [ProgressBar(0, maxGetter: "@m_Attributes.Length",
            ColorGetter = nameof(GetPreviewProgressColor),
            Segmented = true)]
        [OnValueChanged(nameof(OnPreviewProgressValueChanged), InvokeOnInitialize = false, InvokeOnUndoRedo = false)]
        private int m_PreviewProgress = 0;

        [DisableIf("@m_PreviewProgress <= 0")]
        [HorizontalGroup("Preview/Buttons"), Button(name: "Previous")]
        private void PreviewPrevious()
        {
            m_PreviewProgress = Mathf.Clamp(m_PreviewProgress - 1, 0, m_Attributes.Length + 1);
            OnPreviewProgressValueChanged(m_PreviewProgress);
        }
        [DisableIf("@m_Attributes.Length <= m_PreviewProgress")]
        [HorizontalGroup("Preview/Buttons"), Button(name: "Next")]
        private void PreviewNext()
        {
            m_PreviewProgress = Mathf.Clamp(m_PreviewProgress + 1, 0, m_Attributes.Length + 1);
            OnPreviewProgressValueChanged(m_PreviewProgress);
        }
#endif
    }
}