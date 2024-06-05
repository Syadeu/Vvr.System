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
// File created : 2024, 06, 04 23:06

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    [HideMonoScript]
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public abstract class ContentViewEventButton<TEvent> : MonoBehaviour,
        IConnector<IContentViewEventHandler<TEvent>>
        where TEvent: struct, IConvertible
    {
        [SerializeField] private TEvent m_Event;

        private Button m_Button;

        [PublicAPI]
        protected IContentViewEventHandler<TEvent> EventHandler { get; private set; }

        private Button Button
        {
            get
            {
                if (m_Button is null) m_Button = GetComponent<Button>();
                return m_Button;
            }
        }

        protected virtual void Awake()
        {
            Button.onClick.AddListener(OnClick);
        }

        protected virtual void OnClick()
        {
            EventHandler?.ExecuteAsync(m_Event).Forget();
        }

        void IConnector<IContentViewEventHandler<TEvent>>.Connect(IContentViewEventHandler<TEvent> t)
            => EventHandler = t;
        void IConnector<IContentViewEventHandler<TEvent>>.Disconnect(IContentViewEventHandler<TEvent> t)
            => EventHandler = null;
    }
}