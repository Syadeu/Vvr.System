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
// File created : 2024, 05, 11 12:05

#endregion

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace Vvr.Model.Wallet
{
    [Preserve]
    internal class UnresolvedWalletJsonConverter : JsonConverter<UnresolvedWallet>
    {
        public override void WriteJson(JsonWriter writer, UnresolvedWallet value, JsonSerializer serializer)
        {
            value.WriteJson(writer);
        }

        public override UnresolvedWallet ReadJson(JsonReader reader, Type objectType, UnresolvedWallet existingValue, bool hasExistingValue,
            JsonSerializer                         serializer)
        {
            JObject jo = JObject.Load(reader);
            var     o  = new UnresolvedWallet();
            o.ReadJson(jo);
            return o;
        }
    }
}