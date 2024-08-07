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
// File created : 2024, 05, 12 11:05

#endregion

using System;

namespace Vvr.Controller.Condition
{
    public struct ProbabilityResolver
    {
        private Unity.Mathematics.Random m_Random;

        public static ProbabilityResolver Get()
        {
            return new ProbabilityResolver(FNV1a32.Calculate(Guid.NewGuid()));
        }

        private ProbabilityResolver(uint seed)
        {
            m_Random = Unity.Mathematics.Random.CreateFromIndex(seed);
        }

        public bool Resolve(float probability)
        {
            if (probability <= 0 ||
                probability < m_Random.NextFloat(0, 100))
            {
                return false;
            }

            return true;
        }
    }
}