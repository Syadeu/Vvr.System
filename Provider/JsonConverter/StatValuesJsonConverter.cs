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
// File created : 2024, 05, 11 16:05

#endregion

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;
using Vvr.System.Model;

namespace Vvr.System.Provider.JsonConverter
{
    [Preserve]
    internal sealed class StatValuesJsonConverter : JsonConverter<StatValues>
    {
        public override void WriteJson(JsonWriter writer, StatValues value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var item in value)
            {
                writer.WritePropertyName(item.Key.ToString());
                writer.WriteValue(item.Value);
            }
            writer.WriteEndObject();
        }
        public override StatValues ReadJson(JsonReader reader, Type objectType, StatValues existingValue, bool hasExistingValue,
            JsonSerializer                             serializer)
        {
            JObject jo     = JObject.Load(reader);

            var     types  = StatType.None;
            var     values = new float[jo.Count];
            int     index  = 0;
            foreach (var property in jo.Properties())
            {
                StatType t = StatProvider.Static[property.Name];
                types         |= t;
                values[index] =  property.Value.Value<float>();
                index++;
            }

            StatValues result = StatValues.Create(types);
            for (int i = 0; i < values.Length; i++)
            {
                result.Values[i] = values[i];
            }

            return result;
        }
    }
}