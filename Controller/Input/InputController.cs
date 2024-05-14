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
using Vvr.Provider;

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

        public bool EnableControl  { get; set; } = true;
        public bool HasControl { get; private set; }

        public Scope InputScope    => new Scope(this);

        private bool Disposed      { get; set; }


        private IEventTarget m_InputOwner;

        private readonly List<InputControlPredicate> m_Predicates = new();

        private CancellationTokenSource m_CancellationTokenSource;
        private Action                  m_DisposeInterrupters;

        public event InputControlAction OnGainControl;

        public InputControlController()
        {
        }
        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();

            Unregister();

            m_CancellationTokenSource = null;

            Disposed = true;
        }

        public InputControlController Register()
        {
            Vvr.Provider.Provider.Static.Register<IInputControlProvider>(this);
            return this;
        }
        public InputControlController Unregister()
        {
            Vvr.Provider.Provider.Static.Unregister<IInputControlProvider>(this);
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

            if (m_DisposeInterrupters == null)
                m_DisposeInterrupters = () => button.onClick.RemoveListener(d);
            else
                m_DisposeInterrupters += () => button.onClick.RemoveListener(d);
        }

        public async UniTask WaitForInput()
        {
            while (
                EnableControl && m_InputOwner != null &&
                !m_CancellationTokenSource.IsCancellationRequested)
            {
                await UniTask.Yield();
            }
        }

        async UniTask IInputControlProvider.TransferControl(IEventTarget actor)
        {
            m_InputOwner              = actor;
            m_CancellationTokenSource = new();
            HasControl                = true;

            OnGainControl_Impl(actor)
                .Forget();

            "[Input] Wait for input".ToLog();
            await WaitForInput();
        }

        private async UniTask OnGainControl_Impl(IEventTarget target)
        {
            if (OnGainControl == null) return;

            using (InputScope)
            {
                await OnGainControl(target)
                    .AttachExternalCancellation(m_CancellationTokenSource.Token);
            }
        }

        public void Queue()
        {
            Assert.IsNotNull(m_InputOwner);

            m_CancellationTokenSource.Cancel();
            m_DisposeInterrupters?.Invoke();

            HasControl          = false;
            m_InputOwner        = null;
            m_DisposeInterrupters = null;

            "[Input] Queue".ToLog();
        }
    }

    public static class InputControllerExtensions
    {
        public static InputControlController OnlyPlayerActor(this InputControlController t)
        {
            t.AddControlPredicate(x =>
            {
                if (x is not IActor actor) return false;

                return actor.ConditionResolver[Model.Condition.IsPlayerActor](null);
            });
            return t;
        }
    }

    public interface IInputControlProvider : IProvider
    {
        bool    CanControl(IEventTarget target);
        UniTask TransferControl(IEventTarget actor);
    }
}