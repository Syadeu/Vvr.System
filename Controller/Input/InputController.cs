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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Session.World;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Input
{
    public delegate bool InputControlPredicate(IEventTarget target);
    public delegate UniTask InputControlAction(IEventTarget target);

    public class InputControlController : IDisposable, IInputControlProvider
    {
        public struct Scope : IDisposable
        {
            private readonly InputControlController m_ControlController;

            internal Scope(InputControlController ctr)
            {
                m_ControlController = ctr;
            }

            public void Dispose()
            {
                m_ControlController.Queue();
            }
        }

        private UniTaskCompletionSource m_ResetEvent              = new();

        public bool EnableControl  { get; set; } = true;
        public bool HasControl { get; private set; }

        public Scope InputScope    => new Scope(this);

        private bool Disposed      { get; set; }


        private IEventTarget m_InputOwner;

        private readonly List<InputControlPredicate> m_Predicates = new();

        private Action DisposeInterrupters;

        public event InputControlAction OnGainControl;

        public InputControlController()
        {
        }
        public void Dispose()
        {
            m_ResetEvent.TrySetCanceled();

            Unregister();

            m_ResetEvent              = null;

            Disposed = true;
        }

        public InputControlController Register()
        {
            MPC.Provider.Provider.Static.Register(this);
            return this;
        }
        public InputControlController Unregister()
        {
            MPC.Provider.Provider.Static.Unregister(this);
            return this;
        }

        public void AddControlPredicate(InputControlPredicate predicate)
        {
            m_Predicates.Add(predicate);
        }

        public bool CanControl(IEventTarget target)
        {
            return EnableControl && m_Predicates.All(x => x(target));
        }

        public void AddInterrupter(Button button)
        {
            UnityAction d = Queue;
            button.onClick.AddListener(d);

            if (DisposeInterrupters == null)
                DisposeInterrupters = () => button.onClick.RemoveListener(d);
            else
                DisposeInterrupters += () => button.onClick.RemoveListener(d);
        }

        public async UniTask WaitForInput()
        {
            Assert.IsNotNull(m_InputOwner);
            Assert.IsTrue(EnableControl);
            await m_ResetEvent.Task;
        }

        async UniTask IInputControlProvider.TransferControl(IEventTarget actor)
        {
            m_InputOwner   = actor;
            if (m_InputOwner is IConnector<IInputControlProvider> p)
            {
                p.Connect(this);
            }
            HasControl = true;

            OnGainControl_Impl(actor)
                .Forget();

            await WaitForInput();
        }

        private async UniTask OnGainControl_Impl(IEventTarget target)
        {
            if (OnGainControl == null) return;

            await OnGainControl(target);
        }

        public void Queue()
        {
            Assert.IsNotNull(m_InputOwner);

            if (!m_ResetEvent.TrySetResult())
            {
                "??".ToLogError();
            }

            if (m_InputOwner is IConnector<IInputControlProvider> p)
            {
                p.Disconnect();
            }

            DisposeInterrupters?.Invoke();

            m_ResetEvent = new();

            HasControl          = false;
            m_InputOwner        = null;
            DisposeInterrupters = null;
        }
    }

    public interface IInputControlProvider : IProvider
    {
        bool    CanControl(IEventTarget target);
        UniTask TransferControl(IEventTarget actor);
    }
}