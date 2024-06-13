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
// File created : 2024, 05, 24 22:05

#endregion

using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Vvr.Model;

namespace Vvr.Provider
{
    /// <summary>
    /// For saving permanent data
    /// </summary>
    [PublicAPI, LocalProvider]
    public interface IUserDataProvider : IDataProvider
    {

    }

    [PublicAPI, LocalProvider]
    public interface IPlayerPrefDataProvider : IDataProvider
    {

    }

    [PublicAPI, AbstractProvider]
    public interface IDataProvider : IProvider
    {
        int    GetInt(UserDataKey    key, int    defaultValue = 0);
        float  GetFloat(UserDataKey  key, float  defaultValue = 0);
        [CanBeNull] string GetString(UserDataKey key, string defaultValue = null);

        [CanBeNull]
        JToken GetJson(UserDataKey key, JToken defaultValue = null);

        void SetInt(UserDataKey    key, int    value);
        void SetFloat(UserDataKey  key, float  value);
        void SetString(UserDataKey key, [NotNull] string value);
        void SetJson(UserDataKey key, [NotNull] JToken value);
    }
}