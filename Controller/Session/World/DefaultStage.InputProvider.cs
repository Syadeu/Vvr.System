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
// File created : 2024, 05, 14 04:05

#endregion

using System;
using Vvr.Controller.Input;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Session.World
{
    partial class DefaultStage : IConnector<IInputProvider>
    {
        private IInputProvider m_InputProvider;

        void IConnector<IInputProvider>.Connect(IInputProvider t)
        {
            m_InputProvider = t;
        }
        void IConnector<IInputProvider>.Disconnect()
        {
            m_InputProvider = null;
        }
    }
}