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
// File created : 2024, 05, 26 19:05

#endregion

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    /// <summary>
    /// Represents an attribute for the dialogues to specify a portrait on the screen during the dialogue.
    /// </summary>
    [Serializable]
    [DisplayName("Portrait In")]
    internal sealed class DialoguePortraitInAttribute : IDialogueAttribute, IDialoguePreviewAttribute
    {
        [BoxGroup("Portrait", GroupID = "0"), PropertyOrder(-1)]
        [SerializeField, LabelText("Image")] private DialogueAssetReference<DialogueSpeakerPortrait> m_Portrait;

        [BoxGroup("Presentation")]
        [SerializeField] private bool    m_Right;
        [BoxGroup("Presentation")]
        [SerializeField] private Vector2 m_Offset   = new Vector2(100, 0);
        [BoxGroup("Presentation")]
        [SuffixLabel("seconds")]
        [SerializeField] private float   m_Duration = .5f;

        [HideInInspector]
        [SerializeField] private bool m_WaitForCompletion = true;

        async UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            var portraitAsset = await ctx.assetProvider.LoadAsync<DialogueSpeakerPortrait>(m_Portrait.FullPath);
            var portrait      = await ctx.assetProvider.LoadAsync<Sprite>(portraitAsset.Object.Portrait);

            IDialogueViewPortrait target;
            if (m_Right)
                target = ctx.viewProvider.View.RightPortrait;
            else
                target = ctx.viewProvider.View.LeftPortrait;

            UniTask task;
            if (target.WasIn)
                task = target.CrossFadeAndWait(portrait.Object, portraitAsset.Object, m_Duration);
            else
            {
                target.Setup(portrait.Object, portraitAsset.Object);

                Vector2 offset         = m_Offset;
                if (!m_Right) offset.x *= -1f;
                task = target.FadeInAndWait(offset, m_Duration);
            }

            if (m_WaitForCompletion)
                await task;
            else
                ctx.dialogue.RegisterTask(task);
        }

#if UNITY_EDITOR

        [ShowIf(nameof(m_WaitForCompletion))]
        [VerticalGroup("Presentation/0")]
        [Button(ButtonSizes.Medium, DirtyOnClick = true), GUIColor(0, 1, 0)]
        private void WaitForCompletion() => m_WaitForCompletion = false;

        [HideIf(nameof(m_WaitForCompletion))]
        [VerticalGroup("Presentation/0")]
        [Button(ButtonSizes.Medium, DirtyOnClick = true), GUIColor(1, .2f, 0)]
        private void DontWaitForCompletion() => m_WaitForCompletion = true;

        private const string SHARED_ASSET = "Shared Asset";
        [InfoBox("This is shared asset. " +
                 "Anything made changes in here also applies any other references.")]
        [ShowInInspector, ShowIf("@m_Portrait != null")]
        [FoldoutGroup(SHARED_ASSET, GroupID = "0/0")]
        [LabelText(nameof(DialogueSpeakerPortrait.PositionOffset))]
        private Vector3 EditorPortraitPositionOffset
        {
            get => m_Portrait.EditorAsset.PositionOffset;
            set
            {
                m_Portrait.EditorAsset.PositionOffset = value;
                EditorUtility.SetDirty(m_Portrait.EditorAsset);
            }
        }
        [ShowInInspector, ShowIf("@m_Portrait != null")]
        [FoldoutGroup(SHARED_ASSET, GroupID = "0/0")]
        [LabelText(nameof(DialogueSpeakerPortrait.Rotation))]
        private Vector3 EditorPortraitRotation
        {
            get => m_Portrait.EditorAsset.Rotation;
            set
            {
                m_Portrait.EditorAsset.Rotation = value;
                EditorUtility.SetDirty(m_Portrait.EditorAsset);
            }
        }
        [ShowInInspector, ShowIf("@m_Portrait != null")]
        [FoldoutGroup(SHARED_ASSET, GroupID = "0/0")]
        [LabelText(nameof(DialogueSpeakerPortrait.Scale))]
        private Vector3 EditorPortraitScale
        {
            get => m_Portrait.EditorAsset.Scale;
            set
            {
                m_Portrait.EditorAsset.Scale = value;
                EditorUtility.SetDirty(m_Portrait.EditorAsset);
            }
        }

#endif

        public override string ToString()
        {
            string s = m_Right ? "Right" : "Left";
            string n = string.Empty;
#if UNITY_EDITOR
            if (m_Portrait is null || m_Portrait.EditorAsset == null)
                n = string.Empty;
            else
                n = m_Portrait.EditorAsset.name;
#endif

            return $"In {n} {s}: {m_Duration}s";
        }

        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            if (m_Portrait == null || !m_Portrait.IsValid())
            {
                "null return".ToLog();
                return;
            }

            IDialogueViewPortrait target;
            if (m_Right)
                target = view.RightPortrait;
            else
                target = view.LeftPortrait;

            target.Setup(
                m_Portrait.EditorAsset.Portrait.editorAsset as Sprite,
                m_Portrait.EditorAsset);
#endif
        }
    }
}