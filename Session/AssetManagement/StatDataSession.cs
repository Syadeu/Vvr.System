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
// File created : 2024, 06, 08 22:06

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Session.AssetManagement
{
    /// <summary>
    /// StatDataSession is a child session that provides access to stat data.
    /// </summary>
    [UsedImplicitly]
    public sealed class StatDataSession : ChildSession<StatDataSession.SessionData>,
        IStatConditionProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly StatSheet sheet;

            public SessionData(StatSheet s)
            {
                sheet = s;
            }
        }

        public override string DisplayName => nameof(StatDataSession);

        private readonly Dictionary<string, StatType> m_Map = new();

        public StatType this[string t] => m_Map[t];

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            foreach (var row in data.sheet)
            {
                int      i    = row.Index;
                StatType type = (StatType)(1L << i);
                m_Map[row.Id] = type;
            }
            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_Map.Clear();
            return base.OnReserve();
        }

        bool IStatConditionProvider.Resolve(
            IReadOnlyStatValues centerStats,
            IReadOnlyStatValues stats, OperatorCondition condition, string value)
        {
            const char valueDelimiter = '|';
            const char percentChar    = '%';

            using var debugTimer = DebugTimer.Start();

            int i = value.IndexOf(valueDelimiter, StringComparison.Ordinal);
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
            if (indexString[^1] == percentChar)
            {
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