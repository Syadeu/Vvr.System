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
// File created : 2024, 05, 16 22:05

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
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public class TimelineQueueSession : ChildSession<TimelineQueueSession.SessionData>,
        ITimelineQueueProvider,
        IConnector<ICustomMethodProvider>,
        IConnector<IStatConditionProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(TimelineQueueSession);

        private class Entry : IComparable<Entry>, IMethodArgumentResolver, IDisposable
        {
            private readonly CustomMethodDelegate   m_Method;
            private readonly IStatConditionProvider m_StatConditionProvider;

            public IStageActor actor;
            public int         index;

            public float timeOffset;
            public bool  disabled;

            public float Time => m_Method(this);

            public Entry(
                [NotNull] IStatConditionProvider sp,
                [NotNull] CustomMethodDelegate m)
            {
                Assert.IsNotNull(sp);
                Assert.IsNotNull(m, "timeline method is null");

                m_StatConditionProvider = sp;
                m_Method                = m;
            }

            public int CompareTo(Entry other)
            {
                float xx = Time       + timeOffset,
                    yy   = other.Time + other.timeOffset;
                if (Mathf.Approximately(xx, yy))
                {
                    return index.CompareTo(other.index);
                }

                if (xx < yy) return -1;
                return 1;
            }

            float IMethodArgumentResolver.Resolve(string arg)
            {
                StatType s = m_StatConditionProvider[arg];
                return actor.Owner.Stats[s];
            }

            public void Dispose()
            {
                actor = null;
            }
        }

        private readonly HashSet<int>     m_Actors = new();
        private readonly SortedSet<Entry> m_Queue  = new();
        private          int              m_Index;

        private ICustomMethodProvider  m_CustomMethodProvider;
        private IStatConditionProvider m_StatConditionProvider;

        public int  Count         => m_Queue.Count;
        public bool HasAnyEnabled => m_Queue.Count > 0 && m_Queue.Any(x => !x.disabled);

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            m_Actors.Clear();
            m_Queue.Clear();
            return base.OnReserve();
        }

        public int IndexOf(IStageActor actor)
        {
            using var timer = DebugTimer.Start();

            foreach (var entry in m_Queue)
            {
                if (ReferenceEquals(entry.actor, actor))
                    return entry.index;
            }

            return -1;
        }

        public void Enqueue(IStageActor actor)
        {
            using var timer = DebugTimer.Start();

            Assert.IsNotNull(actor);
            Assert.IsNotNull(actor.Owner);

            if (!m_Actors.Add(actor.GetHashCode()))
                throw new InvalidOperationException("duplicated");

            m_Queue.Add(new Entry(
                m_StatConditionProvider,
                m_CustomMethodProvider[CustomMethodNames.TIMELINE])
            {
                actor = actor,
                index = m_Index++,
            });
        }

        public void InsertAfter(int index, IStageActor actor)
        {
            using var timer = DebugTimer.Start();

            Assert.IsNotNull(actor);
            Assert.IsNotNull(actor.Owner);

            if (!m_Actors.Add(actor.GetHashCode()))
                throw new InvalidOperationException("duplicated");

            float targetTimeOffset = 0;
            int   count            = m_Queue.Count;
            var   tempArr          = ArrayPool<Entry>.Shared.Rent(count);
            m_Queue.CopyTo(tempArr, 0);
            m_Queue.Clear();
            for (int i = 0; i < count; i++)
            {
                var entry = tempArr[i];
                if (entry.index > index) entry.index++;

                if (entry.index == index) targetTimeOffset = entry.timeOffset;

                m_Queue.Add(entry);
            }

            ArrayPool<Entry>.Shared.Return(tempArr, true);

            m_Queue.Add(new Entry(
                m_StatConditionProvider,
                m_CustomMethodProvider[CustomMethodNames.TIMELINE])
            {
                actor = actor,
                index = index + 1,
                timeOffset = targetTimeOffset
            });
            m_Index++;
        }

        public IStageActor Dequeue(out float time)
        {
            using var timer = DebugTimer.Start();

            int count = m_Queue.Count;
            if (count == 0) throw new InvalidOperationException("queue empty");

            time = 0;
            IStageActor result = null;
            for (int i = 0; i < count; i++)
            {
                var min = m_Queue.Min;
                m_Queue.Remove(min);
                time           =  min.timeOffset;
                min.timeOffset += min.Time;
                m_Queue.Add(min);

                if (min.disabled) continue;

                result = min.actor;
                break;
            }

            return result;
        }

        public void SetEnable(IStageActor actor, bool enabled)
        {
            using var timer = DebugTimer.Start();

            var e = m_Queue.First(x => ReferenceEquals(x.actor, actor));
            e.disabled = !enabled;
        }

        public bool IsStartFrom(IStageActor actor)
        {
            using var timer = DebugTimer.Start();

            var min = m_Queue.Min;
            return ReferenceEquals(min.actor, actor);
        }

        public void StartFrom(IStageActor actor)
        {
            using var timer = DebugTimer.Start();

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

        public void Remove(IStageActor actor)
        {
            using var timer = DebugTimer.Start();

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

        void IConnector<ICustomMethodProvider>.Connect(ICustomMethodProvider t)
        {
            Assert.IsNull(m_CustomMethodProvider);
            m_CustomMethodProvider = t;
        }
        void IConnector<ICustomMethodProvider>.Disconnect(ICustomMethodProvider t)
        {
            Assert.IsTrue(ReferenceEquals(m_CustomMethodProvider, t));
            m_CustomMethodProvider = null;
        }

        void IConnector<IStatConditionProvider>.Connect(IStatConditionProvider    t) => m_StatConditionProvider = t;
        void IConnector<IStatConditionProvider>.Disconnect(IStatConditionProvider t) => m_StatConditionProvider = null;
    }
}