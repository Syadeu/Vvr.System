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
// File created : 2024, 05, 10 15:05

#endregion

using System.Reflection;
using System.Threading;
using Cathei.BakingSheet.Internal;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace Vvr.Controller.Session
{
    [Preserve, UnityEngine.Scripting.RequireDerived]
    public abstract class ChildSession<TSessionData> : IChildSession
        where TSessionData : ISessionData
    {
        private CancellationTokenSource m_InitializeToken;

        public IParentSession Parent { get; private set; }
        public TSessionData   Data   { get; private set; }

        UniTask IChildSession.Initialize(IParentSession parent, ISessionData data)
        {
            ParentSessionAttribute att = GetType().GetCustomAttribute<ParentSessionAttribute>();
            if (att != null)
            {
                if (att.IncludeInherits)
                {
                    Assert.IsTrue(VvrTypeHelper.InheritsFrom(parent.GetType(), att.Type));
                }
                else Assert.AreEqual(att.Type, parent.GetType());
            }

            if (m_InitializeToken != null)
            {
                m_InitializeToken.Cancel();
                m_InitializeToken.Dispose();
            }

            Parent = parent;
            Data   = data != null ? (TSessionData)data : default;

            m_InitializeToken = new();

            return OnInitialize(parent, Data)
                .AttachExternalCancellation(m_InitializeToken.Token)
                .SuppressCancellationThrow()
                ;
        }
        UniTask IGameSessionBase.Initialize() => UniTask.CompletedTask;

        public async UniTask Reserve()
        {
            m_InitializeToken.Cancel();

            await OnReserve();

            if (Parent is IGameSessionCallback callback)
            {
                callback.OnSessionClosed(this);
            }

            m_InitializeToken.Dispose();
            Parent            = null;
            Data              = default;
            m_InitializeToken = null;
        }

        protected virtual UniTask OnInitialize(IParentSession session, TSessionData data) => UniTask.CompletedTask;
        protected virtual UniTask OnReserve() => UniTask.CompletedTask;
    }
}