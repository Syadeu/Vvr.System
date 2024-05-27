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
// File created : 2024, 05, 26 17:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [CanBeNull]
    public delegate IProvider DialogueProviderResolveDelegate([NotNull] Type providerType);

    [PublicAPI, RequireImplementors]
    public interface IDialogueAttribute
    {
        UniTask ExecuteAsync(
            [NotNull] IDialogueData                   dialogue,
            [NotNull] IAssetProvider                  assetProvider,
            [NotNull] IDialogueViewProvider           viewProvider,
            [NotNull] DialogueProviderResolveDelegate resolveProvider);
    }

    public interface IDialogueSkipAttribute
    {
        bool    CanSkip { get; }
        UniTask OnSkip(
            [NotNull] IDialogueData                   dialogue,
            [NotNull] IAssetProvider                  assetProvider,
            [NotNull] IDialogueViewProvider           viewProvider,
            [NotNull] DialogueProviderResolveDelegate resolveProvider);
    }
}