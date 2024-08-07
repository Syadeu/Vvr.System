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
// File created : 2024, 05, 15 15:05

#endregion

using System;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents custom method names used in the application.
    /// </summary>
    public readonly ref struct CustomMethodNames
    {
        // ReSharper disable InconsistentNaming
        public static CustomMethodNames TIMELINE => new(nameof(TIMELINE));
        // ReSharper restore InconsistentNaming

        private readonly string m_Name;

        private CustomMethodNames(string m) => m_Name = m;

        public override string ToString()    => m_Name;
        public override int    GetHashCode() => unchecked((int)FNV1a32.Calculate(m_Name)) ^ 367;
    }
}