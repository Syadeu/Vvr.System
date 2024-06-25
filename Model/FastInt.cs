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

using System;
using System.Diagnostics;

namespace Vvr.Model
{
    /// <summary>
    /// Represents a fast and efficient way to parse numeric values from strings.
    /// </summary>
    public ref struct FastInt
    {
        public const char Zero = '0';

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private static void EvaluateInputString(ReadOnlySpan<char> value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!char.IsDigit(c))
                {
                    throw new ArgumentException("Invalid input string. Only numeric characters are allowed.");
                }
            }
        }

        /// <summary>
        /// Parses a numeric value from a string.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <returns>The parsed numeric value.</returns>
        public static int Parse(ReadOnlySpan<char> value)
        {
            EvaluateInputString(value);

            int x      = 0;
            int factor = 1;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                int r = value[i] - Zero;
                x      += r * factor;
                factor *= 10;
            }

            return x;
        }
    }
}