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
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public class TimelineQueueSession : ChildSession<TimelineQueueSession.SessionData>,
        ITimelineQueueProvider,
        IConnector<ICustomMethodProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        public override string DisplayName => nameof(TimelineQueueSession);

        private class Entry : IComparable<Entry>
        {
            private readonly CustomMethodDelegate m_Method;

            public IActor actor;
            public int    index;

            public float timeOffset;

            public float Time => m_Method(actor.Stats);

            public Entry(CustomMethodDelegate m)
            {
                m_Method = m;
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
        }

        private readonly HashSet<int>     m_Actors = new();
        private readonly SortedSet<Entry> m_Queue  = new();
        private          int              m_Index;

        private ICustomMethodProvider m_CustomMethodProvider;

        public int Count => m_Queue.Count;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            Parent.Register<ITimelineQueueProvider>(this);

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            Parent.Unregister<ITimelineQueueProvider>();

            return base.OnReserve();
        }

        public int IndexOf(IActor actor)
        {
            foreach (var entry in m_Queue)
            {
                if (ReferenceEquals(entry.actor, actor))
                    return entry.index;
            }

            return -1;
        }

        public void Enqueue(IActor actor)
        {
            Assert.IsNotNull(actor);
            if (!m_Actors.Add(actor.GetHashCode()))
                throw new InvalidOperationException("duplicated");

            m_Queue.Add(new Entry(m_CustomMethodProvider[CustomMethodNames.TIMELINE])
            {
                actor = actor,
                index = m_Index++,
            });
        }

        public void InsertAfter(int index, IActor actor)
        {
            Assert.IsNotNull(actor);

            if (!m_Actors.Add(actor.GetHashCode()))
                throw new InvalidOperationException("duplicated");

            int count   = m_Queue.Count;
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

            m_Queue.Add(new Entry(m_CustomMethodProvider[CustomMethodNames.TIMELINE])
            {
                actor = actor,
                index = index + 1,
            });
            m_Index++;
        }

        public IActor Dequeue()
        {
            if (m_Queue.Count == 0) throw new InvalidOperationException("queue empty");

            var min = m_Queue.Min;
            m_Queue.Remove(min);
            min.timeOffset += min.Time;
            m_Queue.Add(min);

            return min.actor;
        }

        public bool IsStartFrom(IActor actor)
        {
            var min = m_Queue.Min;
            return ReferenceEquals(min.actor, actor);
        }

        public void StartFrom(IActor actor)
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

        public void Remove(IActor actor)
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
    }
}