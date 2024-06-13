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

namespace Vvr.Model
{
#pragma warning disable CS0660, CS0661
    public readonly ref struct UserDataKey
    {
        private readonly string m_Key;

        [UsedImplicitly]
        private UserDataKey(string k)
        {
            m_Key = k;
        }

        public override int GetHashCode()
        {
            return unchecked((int)FNV1a32.Calculate(m_Key)) ^ 267;
        }

        public override string ToString()
        {
            return m_Key;
        }

        public static implicit operator UserDataKey(string t) => new(t);

        public static bool operator ==(UserDataKey x, string y) => x.m_Key == y;
        public static bool operator !=(UserDataKey x, string y) => !(x == y);
    }
#pragma warning restore CS0660, CS0661
}