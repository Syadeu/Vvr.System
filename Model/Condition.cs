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
// File created : 2024, 05, 05 12:05

#endregion

using System;
using System.Text;
using JetBrains.Annotations;

namespace Vvr.Model
{
    /// <summary>
    /// <seealso cref="ConditionResolver"/>
    /// </summary>
    public enum Condition : short
    {
        Always = 0,

        // EventCondition
        OnActorTurn,
        OnActorTurnEnd,
        OnActorDead,

        OnHit,
        OnAttackStart,
        OnBattleStart,
        OnBattleEnd,

        OnTagIn,
        OnTagOut,

        OnSkillStart,
        OnSkillCasting,
        OnSkillEnd,
        OnSkillCooltime,

        OnAbnormalAdded,
        OnAbnormalRemoved,
        OnAbnormalUpdate,
        // EventCondition

        // StateCondition
        IsPlayerActor,
        IsInHand,
        IsActorTurn,
        IsFront,
        // StateCondition

        // AbnormalCondition
        HasAbnormal,
        HasNotAbnormal,
        // AbnormalCondition

        HasPassive,
        HasNotPassive,

        // Operator
        // LogicalCondition
        // ReSharper disable InconsistentNaming
        AND,
        OR,
        // ReSharper restore InconsistentNaming
        // LogicalCondition

        // Logical
        // OperatorCondition
        GEqual,
        LEqual,
        // OperatorCondition

        // Session
        OnFloorStarted,
        OnFloorEnded,
        OnStageStarted,
        OnStageEnded,

        IsFloorStarted,
        IsFloorEnded,
        IsStageStarted,
        IsStageEnded,

        IsFloor,
        IsStage,
        // Session

        // GameConfig
        OnGameConfigStarted,
        OnGameConfigEnded,
        // GameConfig
    }

    public enum SessionEventCondition : short
    {
        OnFloorStarted = Condition.OnFloorStarted,
        OnFloorEnded   = Condition.OnFloorEnded,
    }

    public enum StateCondition : short
    {
        Always = 0,

        IsPlayerActor = Condition.IsPlayerActor,
        IsActorTurn = Condition.IsActorTurn,
        IsInHand    = Condition.IsInHand,
        IsFront = Condition.IsFront
    }

    public enum LogicalCondition : short
    {
        None = 0,

        // ReSharper disable InconsistentNaming
        AND = Condition.AND,
        OR = Condition.OR,
        // ReSharper restore InconsistentNaming
    }

    public readonly struct LogicalOperator
    {
        private readonly short m_Value;

        [UsedImplicitly]
        private LogicalOperator(short v)
        {
            m_Value = v;
        }
        public LogicalOperator(LogicalCondition value)
        {
            switch (value)
            {
                case LogicalCondition.None:
                    m_Value = 0;
                    break;
                case LogicalCondition.AND:
                    m_Value = 0b0001;
                    break;
                case LogicalCondition.OR:
                    m_Value = 0b0010;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public override string ToString()
        {
            if (m_Value == 0) return VvrTypeHelper.Enum<LogicalCondition>.ToString(LogicalCondition.None);

            int           count = 0;
            StringBuilder sb    = new();
            for (int i = 1; i < VvrTypeHelper.Enum<LogicalCondition>.Length; i++)
            {
                short e = (short)(1 << i);
                if ((m_Value & e) == e)
                {
                    if (count > 0) sb.Append(" | ");
                    sb.Append(VvrTypeHelper.Enum<LogicalCondition>.At(i));
                    count++;
                }
            }
            return sb.ToString();
        }

        public static implicit operator LogicalOperator(LogicalCondition t) => new(t);

        public static LogicalOperator operator |(LogicalOperator x, LogicalOperator y)
        {
            return new LogicalOperator((short)(x.m_Value | y.m_Value));
        }
    }

    public enum OperatorCondition : short
    {
        None = 0,

        GEqual = Condition.GEqual,
        LEqual = Condition.LEqual
    }

    public enum EventCondition : short
    {
        None = 0,

        OnActorTurn = Condition.OnActorTurn,
        OnActorTurnEnd = Condition.OnActorTurnEnd,
        OnActorDead = Condition.OnActorDead,

        OnHit = Condition.OnHit,
        OnAttackStart = Condition.OnAttackStart,
        OnBattleStart = Condition.OnBattleStart,
        OnBattleEnd = Condition.OnBattleEnd,

        OnTagIn  = Condition.OnTagIn,
        OnTagOut = Condition.OnTagOut,

        OnSkillStart = Condition.OnSkillStart,
        OnSkillCasting = Condition.OnSkillCasting,
        OnSkillEnd   = Condition.OnSkillEnd,
        OnSkillCooltime = Condition.OnSkillCooltime,

        OnAbnormalAdded = Condition.OnAbnormalAdded,
        OnAbnormalRemoved = Condition.OnAbnormalRemoved,
        OnAbnormalUpdate = Condition.OnAbnormalUpdate,
    }

    public enum AbnormalCondition : short
    {
        None = 0,

        HasAbnormal = Condition.HasAbnormal,
        HasNotAbnormal = Condition.HasNotAbnormal,
    }
}