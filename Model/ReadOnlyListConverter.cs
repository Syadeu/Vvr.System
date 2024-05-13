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
// File created : 2024, 05, 06 00:05

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using Cathei.BakingSheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [Preserve]
    public sealed class ReadOnlyListConverter<T> : SheetValueConverter<IReadOnlyList<T>>
    {
        protected override IReadOnlyList<T> StringToValue(Type type, string value, SheetValueConvertingContext context)
        {
            $"{value}".ToLog();
            JObject jo  = JObject.Parse(value);
            T[]     arr = new T[jo.Count];

            Type t = typeof(T);
            int  i = 0;
            foreach (var item in jo)
            {
                arr[i++] = (T)item.Value.ToObject(t);
            }

            return arr;
        }
        protected override string ValueToString(Type type, IReadOnlyList<T> value, SheetValueConvertingContext context)
        {
            using var sw = new StringWriter();
            using var wr = new JsonTextWriter(sw);

            wr.WriteStartObject();
            for (int i = 0; i < value.Count; i++)
            {
                wr.WritePropertyName($"{i + 1}");
                wr.WriteValue(value[i]);
            }
            wr.WriteEndObject();

            return wr.ToString();
        }
    }
}