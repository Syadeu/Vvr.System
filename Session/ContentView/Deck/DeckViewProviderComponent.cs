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
// File created : 2024, 06, 03 20:06

#endregion

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Provider;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Deck
{
    public abstract class DeckViewProviderComponent
        : ContentViewProviderComponent<DeckViewEvent>, IDeckViewProvider
    {
        private IContentViewEventHandler<DeckViewEvent> m_EventHandler;

        protected IContentViewEventHandler<DeckViewEvent> EventHandler => m_EventHandler;

        public void Initialize(IContentViewEventHandler<DeckViewEvent> eventHandler)
        {
            m_EventHandler = eventHandler;
            OnInitialize(eventHandler);
        }
        public override void Reserve()
        {
            m_EventHandler = null;
        }

        protected virtual void OnInitialize(IContentViewEventHandler<DeckViewEvent> eventHandler){}
    }
}