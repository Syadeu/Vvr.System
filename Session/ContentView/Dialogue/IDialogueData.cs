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
// File created : 2024, 05, 20 23:05

#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Vvr.Model;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.Dialogue
{
    public interface IDialogueData : IRawData
    {
        IReadOnlyList<IDialogueAttribute> Attributes { get; }

        IReadOnlyDictionary<AssetType, AssetReference> Assets { get; }

        IDialogueData NextDialogue { get; }

        void Build(ActorSheet sheet);
    }

    public interface IDialogueSpeaker : IDialogueAttribute
    {
    }

    [Flags]
    public enum DialogueSpeakerOptions
    {
        Left  = 0b0001,
        Right = 0b0010,

        In  = 0b0100,
        Out = 0b1000
    }
}