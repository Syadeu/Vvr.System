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
// File created : 2024, 06, 20 03:06

#endregion

using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    [ProviderSession(typeof(IGameTimeProvider))]
    public sealed class GameTimeSession : ChildSession<GameTimeSession.SessionData>,
        IGameTimeProvider
    {
        public struct SessionData : ISessionData
        {
            public float animateDuration;
        }

        public override string DisplayName => nameof(GameTimeSession);

        private CancellationTokenSource m_CancellationTokenSource;
        private float                   m_TargetTimeScale;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Parent.Register<IGameTimeProvider>(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();

            Parent.Unregister<IGameTimeProvider>();

            return base.OnReserve();
        }

        public void SetTimeScale(float value)
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();

            m_CancellationTokenSource = new();
            Interlocked.Exchange(ref m_TargetTimeScale, value);

            UniTask.Void(SetTimeScaleAsync, m_CancellationTokenSource.Token);
        }

        private async UniTaskVoid SetTimeScaleAsync(CancellationToken cancellationToken)
        {
            if (Data.animateDuration <= 0)
            {
                Time.timeScale = m_TargetTimeScale;
                return;
            }

            Timer timer = Timer.Start();
            float sv    = Time.timeScale;
            while (!timer.IsExceeded(Data.animateDuration) &&
                   !cancellationToken.IsCancellationRequested)
            {
                float t = timer.ElapsedTime / Data.animateDuration;
                Time.timeScale = Mathf.Lerp(sv, m_TargetTimeScale, t);
                await UniTask.Yield();
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            Time.timeScale = m_TargetTimeScale;
        }
    }
}