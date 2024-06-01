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
    internal sealed class DialoguePortraitInAttribute : IDialogueAttribute,
        IDialoguePreviewAttribute, IDialogueRevertPreviewAttribute
    {
#if UNITY_EDITOR
        [BoxGroup("Portrait", GroupID = "0"), PropertyOrder(-1)]
        [LabelText("Image")]
        [InfoBox(
            "Image cannot be null.", InfoMessageType.Error,
            VisibleIf = nameof(EvaluatePortraitImageIsInvalid))]
#endif
        [SerializeField]
        private DialogueAssetReference<DialogueSpeakerPortrait> m_Portrait = new();

        [FoldoutGroup("Presentation")]
        [SerializeField] private bool    m_Right;
        [FoldoutGroup("Presentation")]
        [SerializeField] private Vector2 m_Offset   = new Vector2(100, 0);
        [FoldoutGroup("Presentation")]
        [SuffixLabel("seconds")]
        [SerializeField] private float   m_Duration = .5f;

        [HideInInspector]
        [SerializeField] private bool m_WaitForCompletion = true;

        async UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            UniTask task = ExecutionBody(ctx);

            if (m_WaitForCompletion)
                await task;
            else
                ctx.dialogue.RegisterTask(task);
        }

        private async UniTask ExecutionBody(DialogueAttributeContext ctx)
        {
            var portraitAsset = await ctx.assetProvider.LoadAsync<DialogueSpeakerPortrait>(m_Portrait.FullPath);
            var portrait      = await ctx.assetProvider.LoadAsync<Sprite>(portraitAsset.Object.Portrait);

            var target
                = m_Right ? ctx.viewProvider.View.RightPortrait : ctx.viewProvider.View.LeftPortrait;

            if (target.WasIn)
                await target.CrossFadeAndWait(portrait.Object, portraitAsset.Object, m_Duration);
            else
            {
                target.Setup(portrait.Object, portraitAsset.Object);

                Vector2 offset         = m_Offset;
                if (!m_Right) offset.x *= -1f;
                await target.FadeInAndWait(offset, m_Duration);
            }
        }

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

#if UNITY_EDITOR

        private bool EvaluatePortraitImageIsInvalid()
        {
            if (m_Portrait is null || !m_Portrait.IsValid())
            {
                return true;
            }

            return false;
        }

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
        [ShowIfGroup("0/0", Condition = "@m_Portrait.IsValid()")]
        [FoldoutGroup(SHARED_ASSET, GroupID = "0/0/0")]
        [LabelText(nameof(DialogueSpeakerPortrait.PositionOffset))]
        private Vector2 EditorPortraitPositionOffset
        {
            get
            {
                if (!m_Portrait.IsValid()) return default;
                return m_Portrait.EditorAsset.PositionOffset;
            }
            set
            {
                m_Portrait.EditorAsset.PositionOffset = value;
                EditorUtility.SetDirty(m_Portrait.EditorAsset);
            }
        }

        [ShowInInspector, ShowIf("@m_Portrait != null")]
        [FoldoutGroup(SHARED_ASSET, GroupID = "0/0/0")]
        [LabelText(nameof(DialogueSpeakerPortrait.Rotation))]
        private Vector2 EditorPortraitRotation
        {
            get
            {
                if (!m_Portrait.IsValid()) return default;
                return m_Portrait.EditorAsset.Rotation;
            }
            set
            {
                m_Portrait.EditorAsset.Rotation = value;
                EditorUtility.SetDirty(m_Portrait.EditorAsset);
            }
        }

        [ShowInInspector, ShowIf("@m_Portrait != null")]
        [FoldoutGroup(SHARED_ASSET, GroupID = "0/0/0")]
        [LabelText(nameof(DialogueSpeakerPortrait.Scale))]
        private float EditorPortraitScale
        {
            get
            {
                if (!m_Portrait.IsValid()) return default;
                return m_Portrait.EditorAsset.Scale.x;
            }
            set
            {
                m_Portrait.EditorAsset.Scale = Vector3.one * value;
                EditorUtility.SetDirty(m_Portrait.EditorAsset);
            }
        }

#endif
        void IDialoguePreviewAttribute.Preview(IDialogueView view)
        {
#if UNITY_EDITOR
            if (m_Portrait == null || !m_Portrait.IsValid())
            {
                "null return".ToLog();
                return;
            }

            var target = m_Right ? view.RightPortrait : view.LeftPortrait;

            target.Setup(
                m_Portrait.EditorAsset.EditorPortrait,
                m_Portrait.EditorAsset);
            target.Image.SetAlpha(1);
#endif
        }

        void IDialogueRevertPreviewAttribute.Revert(IDialogueView view)
        {
#if UNITY_EDITOR
            var target = m_Right ? view.RightPortrait : view.LeftPortrait;
            target.Clear();
#endif
        }
    }
}