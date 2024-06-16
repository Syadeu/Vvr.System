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
// File created : 2024, 06, 17 00:06
#endregion

using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.Scripting;
using Vvr.Provider;
using Vvr.Session.ContentView.Dialogue.Attributes;

namespace Vvr.Session.EventView.ActorView
{
    [RequireDerived]
    [Serializable]
    internal abstract class DialogueActorViewAttribute : IDialogueAttribute
    {
        UniTask IDialogueAttribute.ExecuteAsync(DialogueAttributeContext ctx)
        {
            var p = ctx.resolveProvider(VvrTypeHelper.TypeOf<IActorViewProvider>.Type) as IActorViewProvider;

            Assert.NotNull(p);

            return ExecuteAsync(p, ctx);
        }

        protected abstract UniTask ExecuteAsync(IActorViewProvider viewProvider, DialogueAttributeContext ctx);
    }
}