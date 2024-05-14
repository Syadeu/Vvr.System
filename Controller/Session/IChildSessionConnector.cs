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
// File created : 2024, 05, 14 20:05

#endregion

using System;
using Vvr.Provider;

namespace Vvr.Controller.Session
{
    /// <summary>
    /// Connect provider interface to session for internal uses.
    /// </summary>
    internal interface IChildSessionConnector
    {
        /// <summary>
        /// Connects the provider interface to the session for internal uses.
        /// </summary>
        /// <param name="pType">The type of the provider.</param>
        /// <param name="provider">The instance of the provider.</param>
        /// <remarks>
        /// This method connects the specified provider interface to the session for internal use within the ChildSession class.
        /// It is used to establish communication between the session and the provider.
        /// </remarks>
        void Register(Type pType, IProvider provider);
        /// <summary>
        /// Disconnects the provider interface from the session.
        /// </summary>
        /// <param name="pType">The type of the provider interface.</param>
        /// <remarks>
        /// This method disconnects the specified provider interface from the session. It is used to terminate communication between the session and the provider.
        /// </remarks>
        void Unregister(Type pType);
    }
}