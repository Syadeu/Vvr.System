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
// File created : 2024, 05, 07 01:05

#endregion

using System.Buffers;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Crypto;

namespace Vvr.Controller
{
    public struct TimeController
    {
        private static readonly List<ITimeUpdate> s_TimeUpdaters = new();

        public static CryptoFloat CurrentTime { get; private set; } = 0;
        public static bool        IsUpdating  { get; private set; }

        public static void Register(ITimeUpdate t)
        {
            Assert.IsFalse(s_TimeUpdaters.Contains(t));
            s_TimeUpdaters.Add(t);
        }

        public static void Unregister(ITimeUpdate t) => s_TimeUpdaters.Remove(t);

        public static void ResetTime() => CurrentTime = 0;

        public static async UniTask WaitForUpdateCompleted()
        {
            while (IsUpdating)
            {
                await UniTask.Yield();
            }
        }
        public static async UniTask Next(float time)
        {
            IsUpdating  =  true;
            CurrentTime += time;
            float currentTime = CurrentTime;

            // This because time updater container can be changed
            // while updating their state.
            int count       = s_TimeUpdaters.Count;
            var cachedArray = ArrayPool<ITimeUpdate>.Shared.Rent(count);
            s_TimeUpdaters.CopyTo(cachedArray);

            for (int i = 0; i < count; i++)
            {
                var t = cachedArray[i];
                if (!s_TimeUpdaters.Contains(t)) continue;

                await t.OnUpdateTime(currentTime, time);
            }

            ArrayPool<ITimeUpdate>.Shared.Return(cachedArray, true);

            count = s_TimeUpdaters.Count;
            cachedArray = ArrayPool<ITimeUpdate>.Shared.Rent(count);
            s_TimeUpdaters.CopyTo(cachedArray);

            for (int i = 0; i < count; i++)
            {
                var t = cachedArray[i];
                if (!s_TimeUpdaters.Contains(t)) continue;

                await t.OnEndUpdateTime();
            }

            ArrayPool<ITimeUpdate>.Shared.Return(cachedArray, true);

            IsUpdating = false;
        }
    }

    public interface ITimeUpdate
    {
        UniTask OnUpdateTime(float currentTime, float deltaTime);
        UniTask OnEndUpdateTime();
    }
}