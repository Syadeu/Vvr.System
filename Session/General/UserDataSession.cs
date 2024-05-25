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
// File created : 2024, 05, 25 11:05

#endregion

using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class UserDataSession : ChildSession<UserDataSession.SessionData>,
        IUserDataProvider
    {
        public struct SessionData : ISessionData
        {
        }

        // TODO: temp
        private readonly Dictionary<string, object> m_DataStore = new();

        public override string DisplayName => nameof(UserDataSession);

        public int GetInt(UserDataKey key, int defaultValue = 0)
        {
            if (!m_DataStore.TryGetValue(key.ToString(), out var v))
                return defaultValue;

            return (int)v;
        }

        public float GetFloat(UserDataKey key, float defaultValue = 0)
        {
            if (!m_DataStore.TryGetValue(key.ToString(), out var v))
                return defaultValue;

            return (float)v;
        }

        public string GetString(UserDataKey key, string defaultValue = null)
        {
            if (!m_DataStore.TryGetValue(key.ToString(), out var v))
                return defaultValue;

            return (string)v;
        }

        public void SetInt(UserDataKey key, int value)
        {
            m_DataStore[key.ToString()] = value;
        }

        public void SetFloat(UserDataKey key, float value)
        {
            m_DataStore[key.ToString()] = value;
        }

        public void SetString(UserDataKey key, string value)
        {
            m_DataStore[key.ToString()] = value;
        }
    }
}