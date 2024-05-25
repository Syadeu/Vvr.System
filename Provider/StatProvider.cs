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
// File created : 2024, 05, 10 22:05

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Sirenix.Utilities.Editor;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Provider
{
    /// <summary>
    /// Global stat provider
    /// </summary>
    /// <remarks>
    /// Due to stats are immutable, stat types can be held at static.
    /// Because stat type will not be changed during application life-cycle.
    /// </remarks>
    public sealed class StatProvider : IStatConditionProvider
    {
        public static  StatProvider Static { get; private set; }

        [Obsolete]
        public static StatProvider GetOrCreate(StatSheet sheet)
        {
            Static ??= new StatProvider(sheet);
            return Static;
        }

        private readonly Dictionary<string, StatType>        m_Map     = new();
        private readonly Dictionary<StatType, StatSheet.Row> m_DataMap = new();

        public StatSheet.Row this[StatType t] => m_DataMap[t];
        public StatType this[string t] => m_Map[t];

        private StatProvider(StatSheet t)
        {
            foreach (var row in t)
            {
                int      i    = row.Index;
                StatType type = (StatType)(1L << i);
                m_Map[row.Id]   = type;
                m_DataMap[type] = row;
            }
        }

        public bool TryGetType(string id, out StatType result)
        {
            return m_Map.TryGetValue(id, out result);
        }

        bool IStatConditionProvider.Resolve(
            IReadOnlyStatValues centerStats,
            IReadOnlyStatValues stats, OperatorCondition condition, string value)
        {
            const char ValueDelimiter = '|';
            const char PercentChar = '%';

            using var debugTimer = DebugTimer.Start();

            int i = value.IndexOf(ValueDelimiter, StringComparison.Ordinal);
            if (i < 0)
            {
                $"Cannot resolve {condition} {value}".ToLogError();
                return false;
            }

            ReadOnlySpan<char> span = value.AsSpan();

            var statTypeString = span[..i];
            var indexString    = span[(i + 1)..];

            if (!m_Map.TryGetValue(statTypeString.ToString(), out StatType statType))
            {
                $"[Condition] Invalid stat type: {statTypeString.ToString()}".ToLogError();
                return false;
            }

            bool result;
            // var  percentMatch = Regex.Match(indexString, @"([0-9]+)%$");
            // if (percentMatch.Success)
            if (indexString[^1] == PercentChar)
            {
                // float v = float.Parse(percentMatch.Groups[1].Value);
                // float v = float.Parse(
                //     indexString[..^1].ToString()
                //     );
                float v = FastFloat.Parse(indexString[..^1]);

                float percent = stats[statType] / centerStats[statType] * 100;
                switch (condition)
                {
                    case OperatorCondition.GEqual:
                        result = percent >= v;
                        break;
                    case OperatorCondition.LEqual:
                        result = percent <= v;
                        break;
                    case OperatorCondition.None:
                    default:
                        $"[Condition] Invalid logic condition: {condition}, {v}".ToLogError();
                        return false;
                }
            }
            else
            {
                float v = FastFloat.Parse(indexString);
                // if (!float.TryParse(indexString, out float v))
                // {
                //     $"[Condition] Invalid index: {indexString.ToString()}".ToLogError();
                //     return false;
                // }

                switch (condition)
                {
                    case OperatorCondition.GEqual:
                        result = stats[statType] >= v;
                        break;
                    case OperatorCondition.LEqual:
                        result = stats[statType] <= v;
                        break;
                    case OperatorCondition.None:
                    default:
                        $"[Condition] Invalid logic condition: {condition}, {v}".ToLogError();
                        return false;
                }
            }

            // $"[Condition:Stats] Resolved {VvrTypeHelper.Enum<OperatorCondition>.ToString(condition)}: {result}".ToLog();
            return result;
        }
    }
}