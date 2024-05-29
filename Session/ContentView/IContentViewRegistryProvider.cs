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
// File created : 2024, 05, 24 00:05

#endregion

using System;
using Vvr.MPC.Session.ContentView.Research;
using Vvr.Provider;
using Vvr.Session.ContentView.BattleSign;
using Vvr.Session.ContentView.Dialogue;
using Vvr.Session.ContentView.Mainmenu;
using Vvr.Session.ContentView.WorldBackground;

namespace Vvr.Session.ContentView
{
    public interface IContentViewRegistryProvider : IProvider
    {
        IMainmenuViewProvider MainmenuViewProvider { get; }

        IDialogueViewProvider DialogueViewProvider { get; }

        IResearchViewProvider ResearchViewProvider { get; }

        IWorldBackgroundViewProvider WorldBackgroundViewProvider { get; }

        IBattleSignViewProvider BattleSignViewProvider { get; }
    }
}