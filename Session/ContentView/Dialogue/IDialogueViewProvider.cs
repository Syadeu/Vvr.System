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
// File created : 2024, 05, 26 01:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue
{
    [LocalProvider]
    public interface IDialogueViewProvider : IContentViewProvider<DialogueViewEvent>
    {
        IDialogueView View { get; }

        // UniTask SpeakAsync(string dialogueId, [CanBeNull] Sprite portraitImage, IDialogueSpeaker speaker);
    }

    public enum DialogueViewEvent : short
    {
        Open,
        Close,
    }
}