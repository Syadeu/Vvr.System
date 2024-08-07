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
// File created : 2024, 05, 29 17:05

#endregion

using System.Threading;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    [PublicAPI]
    public readonly struct DialogueAttributeContext
    {
        [NotNull] public readonly IDialogue                       dialogue;
        [NotNull] public readonly IAssetProvider                  assetProvider;
        [NotNull] public readonly IDialogueViewProvider           viewProvider;
        [NotNull] public readonly DialogueProviderResolveDelegate resolveProvider;
        [NotNull] public readonly IContentViewEventHandlerProvider       eventHandlerProvider;

        public readonly CancellationToken cancellationToken;

        internal DialogueAttributeContext(
            [NotNull] IDialogue                       dialogue,
            [NotNull] IAssetProvider                  assetProvider,
            [NotNull] IDialogueViewProvider           viewProvider,
            [NotNull] DialogueProviderResolveDelegate resolveProvider,
            [NotNull] IContentViewEventHandlerProvider       eventHandlerProvider,
            CancellationToken cancellationToken
            )
        {
            this.dialogue             = dialogue;
            this.assetProvider        = assetProvider;
            this.viewProvider         = viewProvider;
            this.resolveProvider      = resolveProvider;
            this.eventHandlerProvider = eventHandlerProvider;

            this.cancellationToken = cancellationToken;
        }
    }
}