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
// File created : 2024, 05, 26 15:05

#endregion

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vvr.Model
{
    public interface IDialogueView : IDialogueViewComponent
    {
        IDialogueViewBackground Background { get; }

        IDialogueViewPortrait LeftPortrait { get; }
        IDialogueViewPortrait RightPortrait { get; }

        IDialogueViewText Text { get; }
    }

    public interface IDialogueViewBackground : IDialogueViewImageComponent
    {
    }
    public interface IDialogueViewPortrait : IDialogueViewImageComponent
    {
        void Clear();

        void    Setup(Sprite            portrait, IDialogueSpeaker speaker);
        UniTask CrossFadeAndWait(Sprite sprite,   IDialogueSpeaker speaker, float duration);
        UniTask FadeInAndWait(Vector2   offset,   float            duration);
        UniTask FadeOutAndWait(Vector2  offset,   float            duration);
    }

    public interface IDialogueViewText : IDialogueViewComponent
    {
        void    Clear();
        UniTask SetTextAsync(string    title, string text);
        UniTask AppendTextAsync(string text);
    }
}