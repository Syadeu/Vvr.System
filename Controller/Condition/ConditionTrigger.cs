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
// File created : 2024, 05, 07 03:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Buffer;
using Vvr.Model;
using Vvr.MPC.Provider;

namespace Vvr.Controller.Condition
{
    /// <summary>
    /// Condition trigger for broadcasting all event targets.
    /// </summary>
    public struct ConditionTrigger : IDisposable
    {
        public const string
            Game  = nameof(Game),
            Skill = nameof(Skill),
            Abnormal = nameof(Abnormal)
            ;

        private static readonly List<ConditionTrigger> s_Stack = new();

        /// <summary>
        /// Event that executes any condition has triggered by any event target
        /// </summary>
        public static event Func<IEventTarget, Model.Condition, string, UniTask> OnEventExecutedAsync;

        /// <summary>
        /// Push new condition trigger stack for event target
        /// </summary>
        /// <remarks>
        /// If the event target will trigger any conditions,
        /// ConditionTrigger must be pushed before it.
        /// </remarks>
        /// <param name="target"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static ConditionTrigger Push(IEventTarget target, string displayName = null)
        {
            Assert.IsFalse(target.Disposed);
            // $"[Condition:{target.Owner}:{target.name}] Push trigger stack depth: {s_Stack.Count}".ToLog();

            string path;
            if (!displayName.IsNullOrEmpty()) path = s_Stack.Count > 0 ? $"{s_Stack[^1].m_Path}->{displayName}" : displayName;
            else path                              = s_Stack.Count > 0 ? $"{s_Stack[^1].m_Path}->{target.GetHashCode()}" : target.GetHashCode().ToString();

            // If last stack is already target, return
            if (s_Stack.Count > 0 && s_Stack[^1].m_Target == target)
            {
                var existing = s_Stack[^1];
                return new ConditionTrigger(existing, path);
            }

            var t = new ConditionTrigger(target, path);
            s_Stack.Add(t);
            return t;
        }

        /// <summary>
        /// Find trigger in last of target stack only
        /// </summary>
        /// <param name="target"></param>
        /// <param name="condition"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Last(IEventTarget target, Model.Condition condition, string value)
        {
            var d = s_Stack
                .Where(x => x.m_Target == target)
                .LastOrDefault();

            if (value.IsNullOrEmpty())
                return d.m_Events.Any(x => x.condition == condition);
            return d.m_Events.Any(x => x.condition == condition && x.value == value);
        }

        public static bool Any(IEventTarget target, Model.Condition condition)
        {
            return s_Stack
                .Where(x => x.m_Target == target)
                .SelectMany(x => x.m_Events)
                .Any(x => x.condition == condition);
        }

        public static bool Any(IEventTarget target, Model.Condition condition, string value)
        {
            Event e = new Event(condition, value);
            // if value is null, should check only condition
            if (value.IsNullOrEmpty())
            {
                return s_Stack
                    .Where(x => x.m_Target == target)
                    .SelectMany(x => x.m_Events)
                    .Any(x => x.condition == condition);
            }
            return s_Stack
                .Where(x => x.m_Target == target)
                .Any(x => x.m_Events.Contains(e));
        }
        public static bool Any(IEventTarget target, Model.Condition condition, Predicate<string> predicate)
        {
            return s_Stack
                .Where(x => x.m_Target == target)
                .SelectMany(x => x.m_Events)
                .Any(x => x.condition == condition && predicate(x.value));
        }

        // TODO: make gc allocation free
        [UsedImplicitly]
        private class ConditionQueryWrapper
        {
            public ConditionQuery value;
        }
        private readonly struct Event : IEquatable<Event>
        {
            public readonly Model.Condition condition;
            public readonly string          value;

            private readonly uint m_ValueHash;

            public Event(Model.Condition t, string v)
            {
                condition = t;
                value     = v;
                m_ValueHash = v.IsNullOrEmpty() ? 0 : FNV1a32.Calculate(v);
            }

            public bool Equals(Event other)
            {
                return condition == other.condition && m_ValueHash == other.m_ValueHash;
            }

            public override bool Equals(object obj)
            {
                return obj is Event other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)condition * 397) ^ (int)m_ValueHash;
                }
            }
        }

        private readonly Hash         m_Hash;
        private readonly IEventTarget m_Target;

        private ConditionQueryWrapper m_Conditions;
        private LinkedList<Event>     m_Events;

        private readonly string            m_Path;
        private readonly bool              m_Copied;

        private ConditionTrigger(ConditionTrigger t, string path)
        {
            this     = t;
            m_Path   = path;
            m_Copied = true;
        }
        private ConditionTrigger(IEventTarget ta, string path)
        {
            m_Hash       = Hash.NewHash();
            m_Target     = ta;
            m_Conditions = ObjectPool<ConditionQueryWrapper>.Shared.Get();
            m_Events     = ObjectPool<LinkedList<Event>>.Shared.Get();
            m_Path       = path;
            m_Copied     = false;
        }

        /// <summary>
        /// Execute condition for current event target with value
        /// </summary>
        /// <remarks>
        /// This method also broadcasting to all other related observers.
        /// </remarks>
        /// <param name="condition"></param>
        /// <param name="value"></param>
        public async UniTask Execute(Model.Condition condition, string value)
        {
            Assert.IsFalse(m_Target.Disposed);

            $"[Condition:{m_Target.Owner}:{m_Target.GetHashCode()}({m_Path})] Execute condition({condition}) with {value}".ToLog();
            m_Conditions.value |= condition;
            m_Events.AddLast(new Event(condition, value));

            await ConditionResolver.Execute(m_Target, condition, value);

            if (OnEventExecutedAsync != null)
                await OnEventExecutedAsync.Invoke(m_Target, condition, value);
        }

        public void Dispose()
        {
            if (!m_Copied)
            {
                s_Stack.RemoveAt(s_Stack.Count - 1);
                m_Events.Clear();

                ObjectPool<ConditionQueryWrapper>.Shared.Reserve(m_Conditions);
                ObjectPool<LinkedList<Event>>.Shared.Reserve(m_Events);
            }
            // $"[Condition:{m_Target.Owner}:{m_Target.name}({m_Path})] Pop trigger stack depth: {s_Stack.Count}".ToLog();
            m_Conditions = null;
            m_Events     = null;
        }

        public override int GetHashCode() => unchecked((int)m_Hash.Value);
    }
}