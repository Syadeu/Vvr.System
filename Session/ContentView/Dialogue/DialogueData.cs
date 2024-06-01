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
using UnityEngine.Assertions;
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
#if UNITY_EDITOR
        [Space]
        [ListDrawerSettings(
            AlwaysAddDefaultValue = true,
            ElementColor = nameof(GetAttributeElementColor),
            ShowPaging = false)]
#endif
        [SerializeField]
        private RawDialogueAttribute[] m_Attributes = new RawDialogueAttribute[]
        {
            new (VvrTypeHelper.TypeOf<DialogueWaitForOpened>.Type)
        };

        [Space] [SerializeField]
        private DialogueData m_NextDialogue;

        private IDialogueAttribute[] m_ResolvedAttributes;

        public string Id => name;

        public IReadOnlyList<IDialogueAttribute> Attributes
        {
            get
            {
                if (m_ResolvedAttributes is null)
                {
                    List<IDialogueAttribute> list = new();
                    for (int i = 0; i < m_Attributes.Length; i++)
                    {
                        var e = m_Attributes[i];
                        if (!e.Enabled)
                        {
                            list.Add(null);
                            continue;
                        }

                        list.Add(e.Value);
                    }

                    m_ResolvedAttributes = list.ToArray();
                }
                return m_ResolvedAttributes;
            }
        }
        public IDialogueData NextDialogue => m_NextDialogue;

#if UNITY_EDITOR
        private Color NormalColor => new Color(0.15f, 0.47f, 0.74f);
        private Color ErrorColor => new Color(1, 0.57f, 0.34f);

        private Color GetAttributeElementColor(int index, Color defaultColor)
        {
            var target = m_Attributes[index];
            if (!target.IsValid())
            {
                Color color = new Color(1, 0,0, .5f);
                return color;
            }

            if (0     < m_PreviewProgress &&
                index + 1 <= m_PreviewProgress)
            {
                Color color = GetPreviewProgressColor(index + 1);
                color.a = .5f;

                return color;
            }

            return defaultColor;
        }

        private Color GetPreviewProgressColor(int value)
        {
            if (value               <= 0) return ErrorColor;
            if (m_Attributes.Length <= value - 1) return NormalColor;

            var target = m_Attributes[value - 1];
            if (!target.Enabled) return ErrorColor;

            bool hasPreview = target.Value is IDialoguePreviewAttribute;
            return hasPreview ? NormalColor : ErrorColor;
        }

        private void OnPreviewProgressValueChanged(int value)
        {
            if (Application.isPlaying) return;

            if (m_Attributes.Length <= value - 1) return;

            if (value <= 0)
            {
                PreviewReset();
                return;
            }

            var target = m_Attributes[value - 1];
            if (!target.IsValid() ||
                !target.Enabled ||
                target.Value is not IDialoguePreviewAttribute previewAttribute) return;

            var ins = DialogueViewProviderComponent.EditorPreview();
            if (ins is null) return;

            previewAttribute.Preview(ins);
        }

        [DisableInPlayMode]
        [ShowInInspector, HideLabel]
        [TitleGroup("Preview")]
        [ProgressBar(0, maxGetter: "@m_Attributes.Length",
            ColorGetter = nameof(GetPreviewProgressColor),
            Segmented = true)]
        [OnValueChanged(nameof(OnPreviewProgressValueChanged), InvokeOnInitialize = false, InvokeOnUndoRedo = false)]
        private int m_PreviewProgress = 0;

        [DisableInPlayMode]
        [TitleGroup("Preview")]
        [Button(name: "Reset")]
        private void PreviewReset()
        {
            if (Application.isPlaying) return;

            if (DialogueViewProviderComponent.HasEditorInstance)
            {
                var ins = DialogueViewProviderComponent.EditorPreview();
                Assert.IsNotNull(ins);

                for (int i = m_Attributes.Length - 1; i >= 0; i--)
                {
                    var e = m_Attributes[i];
                    if (e.IsValid() &&
                        e.Enabled &&
                        e.Value is IDialogueRevertPreviewAttribute revert)
                    {
                        revert.Revert(ins);
                    }
                }
            }
            DialogueViewProviderComponent.DestroyEditorPreview();
            // var ins = DialogueViewProviderComponent.EditorPreview();
            // if (ins is null) return;
            //
            // ins.Background.Image.sprite = null;
            // ins.LeftPortrait.Clear();
            // ins.RightPortrait.Clear();
            // ins.Text.Clear();
            //
            m_PreviewProgress = 0;
        }

        [DisableInPlayMode]
        [DisableIf("@m_PreviewProgress <= 0")]
        [HorizontalGroup("Preview/Buttons"), Button(name: "Previous")]
        private void PreviewPrevious()
        {
            if (Application.isPlaying) return;

            var current = m_Attributes[m_PreviewProgress - 1];
            if (current.IsValid())
            {
                var ins = DialogueViewProviderComponent.EditorPreview();
                if (ins is not null &&
                    current.IsValid() && current.Enabled &&
                    current.Value is IDialogueRevertPreviewAttribute revert)
                {
                    revert.Revert(ins);
                }
            }

            m_PreviewProgress = Mathf.Clamp(m_PreviewProgress - 1, 0, m_Attributes.Length + 1);
            OnPreviewProgressValueChanged(m_PreviewProgress);
        }

        [DisableInPlayMode]
        [DisableIf("@m_Attributes.Length <= m_PreviewProgress")]
        [HorizontalGroup("Preview/Buttons"), Button(name: "Next")]
        private void PreviewNext()
        {
            if (Application.isPlaying) return;

            m_PreviewProgress = Mathf.Clamp(m_PreviewProgress + 1, 0, m_Attributes.Length + 1);
            OnPreviewProgressValueChanged(m_PreviewProgress);
        }
#endif
    }
}