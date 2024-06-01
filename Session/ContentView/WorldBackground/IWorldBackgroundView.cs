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
// File created : 2024, 05, 27 11:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Vvr.Session.ContentView.WorldBackground
{
    /// <summary>
    /// Represents the interface for a world background view.
    /// </summary>
    public interface IWorldBackgroundView
    {
        float   Zoom  { get; }
        Vector2 Pan   { get; }
        Image   Image { get; }

        void SetBackground([CanBeNull] Sprite sprite);

        UniTask ZoomAsync(float   zoom, float duration);
        UniTask CenterAsync(float duration);
        UniTask PanAsync(bool     relative, Vector2 offset, float duration);
    }
}