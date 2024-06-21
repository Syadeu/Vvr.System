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
// File created : 2024, 06, 21 20:06
#endregion

using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Provider;
using Vvr.Session.EventView.Core;
using Vvr.Session.EventView.GameObjectPoolView;

namespace Vvr.Session.EventView.EffectView
{
    [UsedImplicitly]
    public class EffectViewSession : ParentSession<EffectViewSession.SessionData>,
        IEffectViewProvider
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(EffectViewSession);

        private IGameObjectPoolViewProvider m_GameObjectPoolViewProvider;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            m_GameObjectPoolViewProvider
                = await CreateSessionOnBackground<GameObjectPoolViewSession>(
                    new GameObjectPoolViewSession.SessionData()
                    {
                        rootObjectName = DisplayName
                    });
        }

        public async UniTask SpawnAsync(
            object key,
            Vector3 position, Quaternion rotation, Transform parent,
            CancellationToken cancellationToken)
        {
            Assert.IsNotNull(key);

            using var cancelTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(ReserveToken, cancellationToken);
            using var scope = await m_GameObjectPoolViewProvider.Scope(key, cancelTokenSource.Token);
            if (cancelTokenSource.IsCancellationRequested)
            {
                return;
            }

            GameObject obj = scope.Object;
            {
                Transform tr = obj.transform;
                if (parent is not null)
                    tr.SetParent(parent, false);
                tr.position = position;
                tr.rotation = rotation;
            }
            obj.SetActive(true);

            var particleSystem = obj.GetComponentInChildren<ParticleSystem>();
            var main           = particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            particleSystem.Play();

            while (!particleSystem.isStopped &&
                   !cancelTokenSource.IsCancellationRequested)
            {
                await UniTask.Yield();
            }

            particleSystem.Stop();
            obj.SetActive(false);
        }
        public async UniTask SpawnAsync(
            object key,
            Vector3 position,
            CancellationToken cancellationToken)
        {
            Assert.IsNotNull(key);

            using var cancelTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(ReserveToken, cancellationToken);
            using var scope = await m_GameObjectPoolViewProvider.Scope(key, cancelTokenSource.Token);
            if (cancelTokenSource.IsCancellationRequested)
            {
                return;
            }

            GameObject obj = scope.Object;
            {
                Transform tr = obj.transform;
                // tr.SetParent(parent, false);
                tr.position = position;
                // tr.rotation = rotation;
            }
            obj.SetActive(true);

            var particleSystem = obj.GetComponentInChildren<ParticleSystem>();
            var main           = particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            particleSystem.Play();

            while (!particleSystem.isStopped &&
                   !cancelTokenSource.IsCancellationRequested)
            {
                await UniTask.Yield();
            }

            particleSystem.Stop();
            obj.SetActive(false);
        }
    }
}