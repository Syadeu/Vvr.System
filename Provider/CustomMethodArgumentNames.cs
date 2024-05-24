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
// File created : 2024, 05, 23 11:05

#endregion

namespace Vvr.Provider
{
#pragma warning disable CS0660, CS0661
    public readonly ref struct CustomMethodArgumentNames
#pragma warning restore CS0660, CS0661
    {
        // ReSharper disable InconsistentNaming
        // ReSharper disable IdentifierTypo
        public static CustomMethodArgumentNames TARGET  => new(nameof(TARGET));
        public static CustomMethodArgumentNames LVL     => new(nameof(LVL));
        public static CustomMethodArgumentNames MAXLVL  => new(nameof(MAXLVL));
        public static CustomMethodArgumentNames NEXTLVL => new(nameof(NEXTLVL));
        // ReSharper restore IdentifierTypo
        // ReSharper restore InconsistentNaming

        private readonly string m_Name;

        private CustomMethodArgumentNames(string m) => m_Name = m;

        public override string ToString()    => m_Name;
        public override int    GetHashCode() => unchecked((int)FNV1a32.Calculate(m_Name)) ^ 367;

        public static bool operator ==(CustomMethodArgumentNames x, string y)
        {
            return x.m_Name == y;
        }
        public static bool operator !=(CustomMethodArgumentNames x, string y)
        {
            return !(x == y);
        }
    }
}