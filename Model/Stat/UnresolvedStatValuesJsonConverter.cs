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
// File created : 2024, 05, 07 03:05

#endregion

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Vvr.Model.Stat
{
    [Preserve]
    internal class UnresolvedStatValuesJsonConverter : JsonConverter<UnresolvedStatValues>
    {
        public override void WriteJson(JsonWriter writer, UnresolvedStatValues value, JsonSerializer serializer)
        {
            value.WriteJson(writer);
        }
        public override UnresolvedStatValues ReadJson(JsonReader reader,           Type           objectType,
            UnresolvedStatValues                                 existingValue,
            bool                                                 hasExistingValue, JsonSerializer serializer)
        {
            JToken jk = JToken.Load(reader);
            if (jk.Type != JTokenType.Object)
            {
                Debug.LogWarning($"Target is not object format but trying to convert {nameof(UnresolvedStatValues)}.");
                return null;
            }

            JObject jo = (JObject)jk;
            var     o  = new UnresolvedStatValues();
            o.ReadJson(jo);
            return o;
        }
    }
}