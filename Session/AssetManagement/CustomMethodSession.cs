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
// File created : 2024, 05, 17 14:05

#endregion

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a custom method session.
    /// </summary>
    /// <remarks>
    /// This session provides custom methods for resolving values.
    /// </remarks>
    [UsedImplicitly]
    [ParentSession(typeof(GameDataSession))]
    public class CustomMethodSession : ChildSession<CustomMethodSession.SessionData>,
        ICustomMethodProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly CustomMethodSheet sheet;

            public SessionData(CustomMethodSheet s)
            {
                sheet = s;
            }
        }

        private readonly Dictionary<int, UnresolvedCustomMethod> m_Methods = new();

        public CustomMethodDelegate this[CustomMethodNames method]
        {
            get
            {
                int hash = method.GetHashCode();
                if (!m_Methods.TryGetValue(hash, out var body))
                {
                    body            = new(Data.sheet[method.ToString()]);
                    m_Methods[hash] = body;
                }

                return body.Execute;
            }
        }

        public override string DisplayName => nameof(CustomMethodSession);
    }
}