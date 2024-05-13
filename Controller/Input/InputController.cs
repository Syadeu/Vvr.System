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
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Session.World;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Input
{
    public class InputController : IDisposable, IInputProvider
    {
        public struct Scope : IDisposable
        {
            private readonly InputController m_Controller;

            internal Scope(InputController ctr)
            {
                m_Controller = ctr;
            }

            public void Dispose()
            {
                m_Controller.Queue();
            }
        }

        private UniTaskCompletionSource m_ResetEvent = new();

        public bool EnableControl  { get; set; } = true;
        public bool InputRequested { get; private set; }

        public Scope InputScope    => new Scope(this);

        private bool Disposed      { get; set; }


        private IActor m_InputOwner;


        public InputController()
        {
        }
        public void Dispose()
        {
            m_ResetEvent.TrySetCanceled();

            Unregister();

            m_ResetEvent = null;

            Disposed = true;
        }

        public InputController Register()
        {
            MPC.Provider.Provider.Static.Register(this);
            return this;
        }
        public InputController Unregister()
        {
            MPC.Provider.Provider.Static.Unregister(this);
            return this;
        }

        public bool CanControl(IActor target)
        {
            return EnableControl && target.ConditionResolver[Model.Condition.IsPlayerActor](null);
        }
        public async UniTask WaitForInput()
        {
            Assert.IsNotNull(m_InputOwner);
            Assert.IsTrue(EnableControl);
            await m_ResetEvent.Task;
        }

        void IInputProvider.Request(IActor actor)
        {
            m_InputOwner   = actor;
            if (m_InputOwner is IConnector<IInputProvider> p)
            {
                p.Connect(this);
            }
            InputRequested = true;
        }

        public void Queue()
        {
            Assert.IsNotNull(m_InputOwner);

            if (!m_ResetEvent.TrySetResult())
            {
                "??".ToLogError();
            }

            if (m_InputOwner is IConnector<IInputProvider> p)
            {
                p.Disconnect();
            }

            m_ResetEvent   = new();
            InputRequested = false;
            m_InputOwner   = null;
        }
    }

    public interface IInputProvider : IProvider
    {
        bool    CanControl(IActor target);
        UniTask WaitForInput();

        void Request(IActor actor);

        void Queue();
    }
}