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
// File created : 2024, 05, 10 01:05

#endregion

using System;
using System.Reflection;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Vvr.MPC.Provider
{
    /// <summary>
    /// Base connector for connects <see cref="Provider"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequireImplementors]
    public interface IConnector<in T> where T : IProvider
    {
        /// <summary>
        /// Method when T has been resolved.
        /// </summary>
        /// <param name="t"></param>
        void Connect(T    t);
        /// <summary>
        /// Method when T has been disconnected.
        /// </summary>
        void Disconnect();
    }
}