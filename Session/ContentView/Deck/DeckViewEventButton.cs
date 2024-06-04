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
// File created : 2024, 06, 04 12:06

#endregion

using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Vvr.Provider;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Deck
{
    [RequireComponent(typeof(Button))]
    [HideMonoScript]
    class DeckViewEventButton : MonoBehaviour, IConnector<IContentViewEventHandler<DeckViewEvent>>
    {
        [SerializeField] private DeckViewEvent m_Event;

        private Button                                  m_Button;
        private IContentViewEventHandler<DeckViewEvent> m_EventHandler;

        private Button Button
        {
            get
            {
                if (m_Button is null) m_Button = GetComponent<Button>();
                return m_Button;
            }
        }

        private void Awake()
        {
            Button.onClick.AddListener(OnClick);
        }
        private void OnClick()
        {
            if (m_EventHandler is null) return;

            m_EventHandler.ExecuteAsync(m_Event).Forget();
        }

        void IConnector<IContentViewEventHandler<DeckViewEvent>>.Connect(IContentViewEventHandler<DeckViewEvent>    t) => m_EventHandler = t;
        void IConnector<IContentViewEventHandler<DeckViewEvent>>.Disconnect(IContentViewEventHandler<DeckViewEvent> t) => m_EventHandler = null;
    }
}