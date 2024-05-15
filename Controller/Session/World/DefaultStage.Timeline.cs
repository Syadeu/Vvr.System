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
// File created : 2024, 05, 12 20:05

#endregion

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.CustomMethod;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Controller.Session.World
{
    partial class DefaultStage
    {
        class TimelineQueue
        {
            class Entry : IComparable<Entry>
            {
                public RuntimeActor   actor;
                public int            index;

                public float time => CustomMethodProvider.Static.Resolve(actor.owner.Stats, CustomMethodNames.TIMELINE);
                public float timeOffset;

                public int CompareTo(Entry other)
                {
                    float xx = time + timeOffset,
                        yy = other.time + other.timeOffset;
                    if (Mathf.Approximately(xx, yy))
                    {
                        return index.CompareTo(other.index);
                    }

                    if (xx < yy) return -1;
                    return 1;
                }
            }

            private readonly HashSet<int>  m_Actors = new();
            private readonly SortedSet<Entry> m_Queue  = new();
            private          int              m_Index;

            public int Count => m_Queue.Count;

            public int IndexOf(RuntimeActor actor)
            {
                foreach (var entry in m_Queue)
                {
                    if (ReferenceEquals(entry.actor, actor))
                        return entry.index;
                }

                return -1;
            }

            public void Enqueue(RuntimeActor actor)
            {
                Assert.IsNotNull(actor);
                if (!m_Actors.Add(actor.GetHashCode()))
                    throw new InvalidOperationException("duplicated");

                m_Queue.Add(new Entry()
                {
                    actor = actor,
                    index = m_Index++,
                });
            }

            public void InsertAfter(int index, RuntimeActor actor)
            {
                Assert.IsNotNull(actor);

                if (!m_Actors.Add(actor.GetHashCode()))
                    throw new InvalidOperationException("duplicated");

                int     count   = m_Queue.Count;
                var tempArr = ArrayPool<Entry>.Shared.Rent(count);
                m_Queue.CopyTo(tempArr, 0);
                m_Queue.Clear();
                for (int i = 0; i < count; i++)
                {
                    var entry = tempArr[i];
                    if (entry.index > index) entry.index++;

                    m_Queue.Add(entry);
                }
                ArrayPool<Entry>.Shared.Return(tempArr, true);

                m_Queue.Add(new Entry()
                {
                    actor = actor,
                    index = index + 1,
                });
                m_Index++;
            }
            public RuntimeActor Dequeue()
            {
                if (m_Queue.Count == 0) throw new InvalidOperationException("queue empty");

                var min = m_Queue.Min;
                m_Queue.Remove(min);
                min.timeOffset += min.time;
                m_Queue.Add(min);

                return min.actor;
            }

            public bool IsStartFrom(RuntimeActor actor)
            {
                var min = m_Queue.Min;
                return ReferenceEquals(min.actor, actor);
            }
            public void StartFrom(RuntimeActor actor)
            {
                Assert.IsNotNull(actor);

                if (IsStartFrom(actor)) return;

                var targetEntry = m_Queue.FirstOrDefault(x => ReferenceEquals(x.actor, actor));
                if (targetEntry == null)
                    throw new InvalidOperationException("Actor not found.");

                int count   = m_Queue.Count;
                var tempArr = ArrayPool<Entry>.Shared.Rent(count);
                m_Queue.CopyTo(tempArr, 0);
                m_Queue.Clear();

                int targetIndex = targetEntry.index;
                for (int i = 0; i < count; i++)
                {
                    var entry = tempArr[i];
                    if (ReferenceEquals(entry.actor, actor))
                    {
                        entry.index = 0;
                    }
                    else if (entry.index < targetIndex)
                    {
                        entry.index++;
                    }

                    m_Queue.Add(entry);
                }

                ArrayPool<Entry>.Shared.Return(tempArr, true);
            }

            public void Remove(RuntimeActor actor)
            {
                if (!m_Actors.Remove(actor.GetHashCode()))
                    return;

                m_Queue.RemoveWhere(x => ReferenceEquals(x.actor, actor));
            }

            public void Clear()
            {
                m_Actors.Clear();
                m_Queue.Clear();
                m_Index = 0;
            }
        }

        private readonly TimelineQueue m_TimelineQueue = new();
        private readonly ActorList     m_Timeline      = new();

        private partial void DequeueTimeline()
        {
            if (m_Timeline.Count > 0) m_Timeline.RemoveAt(0);

            UpdateTimeline();
        }
        private partial void UpdateTimeline()
        {
            const int maxTimelineCount = 5;

            if (m_Timeline.Count > 0 && !m_TimelineQueue.IsStartFrom(m_Timeline[0]))
            {
                m_TimelineQueue.StartFrom(m_Timeline[0]);
                for (int i = m_Timeline.Count - 1; i >= 1; i--)
                {
                    m_Timeline.RemoveAt(i);
                }
            }

            while (m_TimelineQueue.Count > 0 && m_Timeline.Count < maxTimelineCount)
            {
                m_Timeline.Add(m_TimelineQueue.Dequeue());
            }
        }

        private partial async UniTask Join(ActorList field, RuntimeActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor, ActorPositionComparer.Static);

            var viewProvider = await m_ViewProvider;
            var view         = await viewProvider.Resolve(actor.owner);

            bool    isFront = ResolvePosition(field, actor);
            Vector3 pos     = view.localPosition;
            pos.z              = isFront ? 1 : 0;
            view.localPosition = pos;

            m_TimelineQueue.Enqueue(actor);
        }
        private partial async UniTask JoinAfter(RuntimeActor target, ActorList field, RuntimeActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor, ActorPositionComparer.Static);

            int index = m_TimelineQueue.IndexOf(target);
            m_TimelineQueue.InsertAfter(index, actor);

            using (var trigger = ConditionTrigger.Push(actor.owner, ConditionTrigger.Game))
            {
                await trigger.Execute(Model.Condition.OnTagIn, null);
            }
        }

        private partial async UniTask Delete(ActorList field, RuntimeActor actor)
        {
            bool result = field.Remove(actor);
            Assert.IsTrue(result);

            await RemoveFromTimeline(actor);
            await RemoveFromQueue(actor);

            var viewProvider = await m_ViewProvider;
            await viewProvider.Release(actor.owner);
            actor.owner.Release();

            UpdateTimeline();
        }
        private partial async UniTask RemoveFromQueue(RuntimeActor actor)
        {
            m_TimelineQueue.Remove(actor);
        }
        private partial async UniTask RemoveFromTimeline(RuntimeActor actor, int preserveCount = 0)
        {
            for (int i = 0; i < m_Timeline.Count; i++)
            {
                var e = m_Timeline[i];
                if (e.owner != actor.owner) continue;

                if (0 < preserveCount--) continue;

                m_Timeline.RemoveAt(i);
                i--;
            }
        }

        [MustUseReturnValue]
        private bool ResolvePosition(IList<RuntimeActor> field, IRuntimeActor runtimeActor)
        {
            int count = field.Count;
            // If no actor in the field, always front
            if (count == 0) return true;

            // This because field list is ordered list by ActorPositionComparer.
            // If the first element is defensive(2), should direct comparison with given actor
            if (field[0].data.Type == ActorSheet.ActorType.Defensive)
            {
                int order = ActorPositionComparer.Static.Compare(runtimeActor, field[0]);
                // If only actor is Offensive(0)
                if (order < 0) return false;
                // If actor also defensive(2)
                if (order == 0)
                {
                    return true;
                }
                return false;
            }

            var lastActor = field[^1];
            // If first is not defensive, should iterate all fields until higher order found
            for (int i = 0; i < count; i++)
            {
                RuntimeActor e    = field[i];
                int order = ActorPositionComparer.Static.Compare(runtimeActor, e);
                if (order == 0) continue;

                // If e is default or defensive will return -1.
                // In that case, this actor must be front
                if (order < 0) return true;

                // If given actor is default or defensive,
                order = ActorPositionComparer.Static.Compare(runtimeActor, lastActor);
                // If last is defence, and actor is default
                // should front of it
                if (order < 0) return true;

                // and field has only default or offensive should behind of it.
                return false;
            }

            // If all check failed, given actor should be front.
            return true;
        }
    }
}