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
// File created : 2024, 05, 27 16:05

#endregion

using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Provider;
using Vvr.Session.ContentView.Core;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.ContentView.BattleSign
{
    [Serializable]
    [DisplayName("Battle Sign Play")]
    class DialoguePlayBattleSignAttribute : IDialogueAttribute
    {
        [SerializeField] private string m_Text;
        [SerializeField] private bool   m_WaitForClose = true;

        public async UniTask ExecuteAsync(DialogueAttributeContext ctx)
        {
            var provider = ctx.resolveProvider(
                VvrTypeHelper.TypeOf<IBattleSignViewProvider>.Type) as IBattleSignViewProvider;
            Assert.IsNotNull(provider, "provider != null");

            var canvas = ctx.resolveProvider(VvrTypeHelper.TypeOf<ICanvasViewProvider>.Type) as ICanvasViewProvider;
            if (canvas is null)
                throw new InvalidOperationException("canvas is null");

            if (m_WaitForClose)
                await provider.OpenAsync(canvas, ctx.assetProvider, m_Text, ctx.cancellationToken);
            else
                provider.OpenAsync(canvas, ctx.assetProvider, m_Text, ctx.cancellationToken).Forget();
        }

        public override string ToString()
        {
            return m_Text;
        }
    }
}