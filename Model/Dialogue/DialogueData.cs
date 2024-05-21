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

using System;
using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Vvr.Model.Dialogue;

namespace Vvr.Model
{
    [CreateAssetMenu(menuName = "Vvr/Create DialogueData", fileName = "DialogueData", order = 0)]
    public class DialogueData : ScriptableObject, IDialogueData, IDisposable
    {
        [SerializeField] private int    m_Index;

        [SerializeField] private DialogueSpeaker[]    m_Speakers;
        [SerializeField] private AssetReferenceSprite m_BackgroundImage;

        private readonly Dictionary<AssetType, AssetReference> m_Assets = new();

        public string Id => name;
        public int Index => m_Index;

        public IReadOnlyList<IDialogueSpeakerData> Speakers => m_Speakers;
        public IReadOnlyDictionary<AssetType, AssetReference> Assets => m_Assets;

        public void Build(ActorSheet sheet)
        {
            foreach (var speaker in m_Speakers)
            {
                speaker.Build(sheet);
            }

            m_Assets[AssetType.BackgroundImage] = m_BackgroundImage;
        }

        public void Dispose()
        {
            foreach (var speaker in m_Speakers)
            {
                speaker.Dispose();
            }
            m_Assets.Clear();
        }
    }

    [Serializable]
    public class DialogueSpeaker : IDialogueSpeakerData, IDisposable
    {
        [HorizontalGroup("Info"), LabelWidth(50)]
        [SerializeField] private string m_Actor;
        [HorizontalGroup("Info"), LabelWidth(20)]
        [SerializeField, HideLabel] private int    m_Id;

        [Space] [BoxGroup("Definition")] [SerializeField]
        private float m_Time;
        [BoxGroup("Definition")]
        [SerializeField]
        private DialogueSpeakerOptions m_Options = DialogueSpeakerOptions.Left | DialogueSpeakerOptions.In;

        [Space] [SerializeField, TextArea] private string m_Message;

        [Space]
        [SerializeField] private DialogueSpeakerPortrait m_PortraitReference;
        [ShowIf("@" + nameof(m_PortraitReference) + " == null")]
        [SerializeField] private AssetReferenceSprite    m_OverridePortrait;
        [ShowIf("@" + nameof(m_PortraitReference) + " == null")]
        [SerializeField] private Vector3                 m_PositionOffset;
        [ShowIf("@" + nameof(m_PortraitReference) + " == null")]
        [SerializeField] private Vector3                 m_Rotation;
        [ShowIf("@" + nameof(m_PortraitReference) + " == null")]
        [SerializeField] private Vector3                 m_Scale = Vector3.one;

        private IActorData m_ResolvedActor;

        public int        Id    => m_Id;
        public IActorData Actor => m_ResolvedActor;

        public string                 Message          => m_Message;
        public float                  Time             => m_Time;

        public DialogueSpeakerOptions Options          => m_Options;

        public AssetReferenceSprite OverridePortrait =>
            m_PortraitReference == null ? m_OverridePortrait : m_PortraitReference.Portrait;
        public Vector3 PositionOffset =>
            m_PortraitReference == null ? m_PositionOffset : m_PortraitReference.PositionOffset;
        public Vector3 Rotation => m_PortraitReference == null ? m_Rotation : m_PortraitReference.Rotation;
        public Vector3 Scale => m_PortraitReference == null ? m_Scale : m_PortraitReference.Scale;


        public IDialogueAttribute     Attribute        { get; } = null;

        public void Build(ActorSheet sheet)
        {
            if (!m_Actor.IsNullOrEmpty())
                m_ResolvedActor = sheet[m_Actor];
        }

        public void Dispose()
        {
            m_ResolvedActor = null;
        }
    }
}