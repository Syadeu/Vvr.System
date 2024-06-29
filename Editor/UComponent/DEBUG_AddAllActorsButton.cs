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
// File created : 2024, 06, 17 12:06
#endregion

using UnityEngine;
using UnityEngine.UI;
using Vvr.Provider;
using Vvr.Session.Provider;
using Vvr.Session.World;
using Vvr.TestClass;

namespace Vvr.UComponent
{
    [RequireComponent(typeof(Button))]
    class DEBUG_AddAllActorsButton : DebugComponent
    {
        [SerializeField] private string m_PrefixId = "CH";

        private Button m_Button;

        protected override void OnAwake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            var p = GameWorld.World.GetProviderRecursive<IUserActorProvider>();
            p.Enqueue(new AddAllExistingActorQueryCommand(m_PrefixId));
        }
    }
}