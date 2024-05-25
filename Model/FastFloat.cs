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
// File created : 2024, 05, 25 12:05

#endregion

using System;
using System.Diagnostics;

namespace Vvr.Model
{
    public ref struct FastFloat
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private static void EvaluateInputString(ReadOnlySpan<char> value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!char.IsDigit(c) && c != '.')
                {
                    throw new ArgumentException("Invalid input string. Only numeric characters and '.' are allowed.");
                }
            }
        }
        public static float Parse(ReadOnlySpan<char> value)
        {
            const char delimiter = '.';

            EvaluateInputString(value);

            int index = value.IndexOf(delimiter);
            if (index < 0) return FastInt.Parse(value);

            var v0 = value[..index];
            var v1 = value[(index + 1)..];

            int x = FastInt.Parse(v0);

            float y           = 0;
            float floatFactor = 0.1f;
            for (int i = 0; i < v1.Length; i++)
            {
                int r = v1[i] - FastInt.Zero;
                y           += r * floatFactor;
                floatFactor *= 0.1f;
            }

            return x + y;
        }
    }
}