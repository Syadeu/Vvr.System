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
// File created : 2024, 05, 17 23:05

#endregion

using System;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Vvr.Controller.Skill
{
    public interface ISkillEffectEmitter
    {
        UniTask Execute(Vector3 position);
        UniTask Execute(Vector3 position, Quaternion rotation);

        UniTask Stop();
    }

    [Obsolete("Because using outside of Session design")]
    internal sealed class SkillEffectEmitter : ISkillEffectEmitter
    {
        private readonly AddressablePath m_Path;

        private IEffectObject m_CurrentEffect;

        public SkillEffectEmitter(AddressablePath path)
        {
            m_Path = path;
        }

        public async UniTask Execute(Vector3 position, Quaternion rotation)
        {
            if (m_Path.FullPath.IsNullOrEmpty()) return;

            var effectPool = GameObjectPool.Get(m_Path);
            m_CurrentEffect = await effectPool.SpawnEffect(position, rotation);
            while (!m_CurrentEffect.Reserved)
            {
                await UniTask.Yield();
            }
        }
        public async UniTask Execute(Vector3 position)
        {
            if (m_Path.FullPath.IsNullOrEmpty()) return;

            var effectPool = GameObjectPool.Get(m_Path);
            m_CurrentEffect = await effectPool.SpawnEffect(position);
            while (!m_CurrentEffect.Reserved)
            {
                await UniTask.Yield();
            }
        }

        public async UniTask Stop()
        {
            m_CurrentEffect.Stop();

            while (!m_CurrentEffect.Reserved)
            {
                await UniTask.Yield();
            }
        }
    }

    public sealed class CustomSkillEffectEmitter : ISkillEffectEmitter
    {
        private readonly AssetReference m_Path;

        private IEffectObject m_CurrentEffect;

        public CustomSkillEffectEmitter(AssetReference path)
        {
            m_Path = path;
        }

        public async UniTask Execute(Vector3 position, Quaternion rotation)
        {
            if (!m_Path.RuntimeKeyIsValid()) return;

            var effectPool = GameObjectPool.GetWithRawKey(m_Path);
            m_CurrentEffect = await effectPool.SpawnEffect(position, rotation);
            while (!m_CurrentEffect.Reserved)
            {
                await UniTask.Yield();
            }
        }

        public async UniTask Execute(Vector3 position)
        {
            if (!m_Path.RuntimeKeyIsValid()) return;

            var effectPool = GameObjectPool.GetWithRawKey(m_Path);
            m_CurrentEffect = await effectPool.SpawnEffect(position);
            while (!m_CurrentEffect.Reserved)
            {
                await UniTask.Yield();
            }
        }

        public async UniTask Stop()
        {
            if (m_CurrentEffect == null) return;
            
            m_CurrentEffect.Stop();

            while (!m_CurrentEffect.Reserved)
            {
                await UniTask.Yield();
            }

            m_CurrentEffect = null;
        }
    }
}