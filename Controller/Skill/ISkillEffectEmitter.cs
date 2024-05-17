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

namespace Vvr.Controller.Skill
{
    public interface ISkillEffectEmitter
    {
        UniTask Execute(Vector3 position, Quaternion rotation);
    }

    [Obsolete("Because using outside of Session design")]
    internal sealed class SkillEffectEmitter : ISkillEffectEmitter
    {
        private readonly AddressablePath m_Path;

        public SkillEffectEmitter(AddressablePath path)
        {
            m_Path = path;
        }

        public async UniTask Execute(Vector3 position, Quaternion rotation)
        {
            var effectPool = GameObjectPool.Get(m_Path);
            var effect     = await effectPool.SpawnEffect(position, rotation);
            while (!effect.Reserved)
            {
                await UniTask.Yield();
            }
        }
    }
}