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
// File created : 2024, 06, 13 20:06

#endregion

using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class PlayerPrefDataSession : ChildSession<PlayerPrefDataSession.SessionData>,
        IPlayerPrefDataProvider
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(PlayerPrefDataSession);

        public int GetInt(UserDataKey key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key.GetHashCode().ToString(), defaultValue);
        }

        public float GetFloat(UserDataKey key, float defaultValue = 0)
        {
            return PlayerPrefs.GetFloat(key.GetHashCode().ToString(), defaultValue);
        }

        public string GetString(UserDataKey key, string defaultValue = null)
        {
            return PlayerPrefs.GetString(key.GetHashCode().ToString(), defaultValue);
        }

        [CanBeNull]
        public JToken GetJson(UserDataKey key, JToken defaultValue = null)
        {
            string v = GetString(key, string.Empty);
            if (v is null || v.IsNullOrEmpty()) return null;

            return JToken.Parse(v);
        }

        public void SetInt(UserDataKey key, int value)
        {
            PlayerPrefs.SetInt(key.GetHashCode().ToString(), value);
        }

        public void SetFloat(UserDataKey key, float value)
        {
            PlayerPrefs.SetFloat(key.GetHashCode().ToString(), value);
        }

        public void SetString(UserDataKey key, string value)
        {
            PlayerPrefs.SetString(key.GetHashCode().ToString(), value);
        }

        public void SetJson(UserDataKey key, JToken jo)
        {
            SetString(key, jo.ToString(Formatting.None));
        }
    }
}