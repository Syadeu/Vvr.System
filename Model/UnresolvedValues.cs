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
// File created : 2024, 05, 11 15:05

#endregion

using Cathei.BakingSheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vvr.System.Model
{
    internal abstract class UnresolvedValues<TValue>
    {
        public string[] ids;
        public float[]  values;

        public TValue Value { get; private set; }

        private void Set(int i, string x, float v)
        {
            ids[i]    = x;
            values[i] = v;
        }

        public void ReadJson(JObject jo)
        {
            ids    = new string[jo.Count];
            values = new float[jo.Count];

            int     i  = 0;
            foreach (var item in jo)
            {
                Set(i++, item.Key, item.Value.Value<float>());
            }
        }

        public void WriteJson(JsonWriter writer)
        {
            writer.WriteStartObject();

            for (int i = 0; i < values.Length; i++)
            {
                writer.WritePropertyName(ids[i].ToString());
                writer.WriteValue(values[i]);
            }

            writer.WriteEndObject();
        }

        public string Write()
        {
            JObject   jo = new JObject();
            using var wr = jo.CreateWriter();
            WriteJson(wr);

            return jo.ToString(Formatting.None);
        }

        public void Build(ISheet sheet)
        {
            Value = Resolve(sheet);
        }
        protected abstract TValue Resolve(ISheet sheet);
    }
}