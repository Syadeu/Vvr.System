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
// File created : 2024, 05, 05 16:05

#endregion

using System;

namespace Vvr.Model.Stat
{
    [Flags]
    public enum StatType : long
    {
        // ReSharper disable InconsistentNaming
        None = 0,

        SPD = 0b0001,
        ATT = 0b0010,
        DEF = 0b0100,
        ARM = 0b1000,
        HP  = 0b0001_0000,
        SHD  = 0b0010_0000,

        // ReSharper restore InconsistentNaming
    }

    public static class StatTypeExtensions
    {
        public static int Count(this StatType t)
        {
            long x     = (long)t;
            int  count = 0;
            while (x != 0)
            {
                count++;
                x &= (x - 1);
            }

            return count;
        }
    }
}