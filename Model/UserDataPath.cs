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

using System.Text;
using System.Threading;
using JetBrains.Annotations;

namespace Vvr.Model
{
    /// <summary>
    /// Represents a collection of user data keys related to game configuration and research.
    /// </summary>
    [PublicAPI]
    public readonly ref struct UserDataPath
    {
        public const char Delimiter = '_';

        private static readonly ThreadLocal<StringBuilder> s_StringBuilder
            = new(() => new StringBuilder(256));

        private static string KeyFormatter(string x, string y)
        {
            StringBuilder sb = s_StringBuilder.Value;
            sb.Clear();
            sb.Append(x);
            sb.Append(Delimiter);
            sb.Append(y);
            return sb.ToString();
        }

        public readonly ref struct Actor
        {
            public const int TeamCount = 3;

            public static UserDataKey UserActors()
            {
                return KeyFormatter(nameof(Actor), nameof(UserActors));
            }
            public static UserDataKey CurrentTeam()
            {
                return KeyFormatter(nameof(Actor), nameof(CurrentTeam));
            }
        }

        /// <summary>
        /// Represents the game configuration related to user data keys.
        /// </summary>
        public readonly ref struct GameConfig
        {
            public static UserDataKey ExecutedCount(string id)
            {
                string obj = KeyFormatter(nameof(GameConfig), id);
                return KeyFormatter(obj, nameof(ExecutedCount));
            }
        }

        /// <summary>
        /// Represents a collection of user data keys related to game configuration and research.
        /// </summary>
        public readonly ref struct Research
        {
            public static UserDataKey NodeLevel(string id)
            {
                string obj = KeyFormatter(nameof(Research), id);
                return KeyFormatter(obj, nameof(NodeLevel));
            }
        }
    }
}