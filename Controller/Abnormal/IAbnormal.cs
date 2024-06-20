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
// File created : 2024, 05, 08 01:05

#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Controller.Actor;
using Vvr.Model;

namespace Vvr.Controller.Abnormal
{
    /// <summary>
    /// Abnormal controller
    /// </summary>
    public interface IAbnormal : IEnumerable<IReadOnlyRuntimeAbnormal>
    {
        IActor Owner { get; }
        int    Count { get; }

        void Clear();
        /// <summary>
        /// Add raw abnormal data to process
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [PublicAPI]
        UniTask AddAsync(AbnormalSheet.Row data);
        /// <summary>
        /// Returns given abnormal id is in this controller
        /// </summary>
        /// <param name="abnormalId"></param>
        /// <returns></returns>
        [PublicAPI]
        bool Contains(Hash       abnormalId);
    }
}