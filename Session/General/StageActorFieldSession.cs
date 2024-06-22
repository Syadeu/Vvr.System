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
// File created : 2024, 06, 21 14:06

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class StageActorFieldSession : ChildSession<StageActorFieldSession.SessionData>,
        IStageActorField, IReadOnlyActorList
    {
        public struct SessionData : ISessionData
        {
            public Owner? Owner { get; set; }
        }
        struct ActorPositionComparer : IComparer<IStageActor>
        {
            // public static readonly Func<IStageActor, IStageActor> Selector = x => x;
            public static readonly IComparer<IStageActor> Static = default(ActorPositionComparer);

            public int Compare(IStageActor x, IStageActor y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                short xx = (short)x.Data.Type,
                    yy   = (short)y.Data.Type;

                // if (xx == yy)
                // {
                //     return x.TargetingPriority.CompareTo(y.TargetingPriority);
                // }
                if (xx < yy) return 1;
                return xx > yy ? -1 : 0;
            }
        }
        struct TargetingPriorityComparer : IComparer<IStageActor>
        {
            // public static readonly Func<IStageActor, IStageActor> Selector = x => x;
            public static readonly IComparer<IStageActor> Static = default(TargetingPriorityComparer);

            public int Compare(IStageActor x, IStageActor y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                short xx = (short)x.Data.Type,
                    yy   = (short)y.Data.Type;

                if (xx == yy)
                {
                    if (x.TargetingPriority < y.TargetingPriority)
                        return 1;
                    if (x.TargetingPriority > y.TargetingPriority)
                        return -1;
                    return 0;
                }
                if (xx < yy) return 1;
                return xx > yy ? -1 : 0;
            }
        }

        public override string DisplayName => nameof(StageActorFieldSession);

        Owner IStageActorField.Owner => Data.Owner ?? Owner;

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public IStageActor this[int index]
        {
            get
            {
                using var wl = new SemaphoreSlimLock(m_Lock);
                wl.Wait(TimeSpan.FromSeconds(1));

                return m_Actors[index];
            }
            set
            {
                using var wl = new SemaphoreSlimLock(m_Lock);
                wl.Wait(TimeSpan.FromSeconds(1));

                m_Actors[index] = value;
            }
        }

        public int                    Count      => m_Actors.Count;
        bool ICollection<IStageActor>.IsReadOnly => false;

        private readonly List<IStageActor> m_Actors = new();
        private readonly SemaphoreSlim     m_Lock   = new(1, 1);

        protected override UniTask OnReserve()
        {
            Clear();

            m_Lock.Dispose();
            return base.OnReserve();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void Add(IStageActor actor)
        {
            Assert.IsNotNull(actor);
            Assert.IsTrue(actor.Owner.Owner == ((IStageActorField)this).Owner);

            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            m_Actors.Add(actor, ActorPositionComparer.Static);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void Clear()
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            m_Actors.Clear();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public bool Contains(IStageActor item)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            return m_Actors.Contains(item);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public bool TryGetActor(string instanceId, out IStageActor actor)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            for (int i = 0; i < m_Actors.Count; i++)
            {
                if (m_Actors[i].Owner.GetInstanceID().ToString() == instanceId)
                {
                    actor = (m_Actors[i]);
                    return true;
                }
            }

            actor = default;
            return false;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public int FindIndex(IActor actor)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            for (int i = 0; i < m_Actors.Count; i++)
            {
                var e = m_Actors[i];
                if (ReferenceEquals(e.Owner, actor)) return i;
            }

            throw new InvalidOperationException();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public int IndexOf(IStageActor item)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            return m_Actors.IndexOf(item);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void Insert(int index, IStageActor item)
        {
            Assert.IsNotNull(item);
            Assert.IsTrue(item.Owner.Owner == ((IStageActorField)this).Owner);

            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            m_Actors.Insert(index, item);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public bool Remove(IStageActor item)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            return m_Actors.Remove(item);
        }
        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void RemoveAt(int index)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            m_Actors.RemoveAt(index);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public bool TryGetActor(IActor actor, out IStageActor result)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            for (int i = 0; i < m_Actors.Count; i++)
            {
                result = m_Actors[i];
                if (!ReferenceEquals(result.Owner, actor)) continue;

                return true;
            }

            result = null;
            return false;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void CopyTo(IStageActor[] array)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            for (int i = 0; i < m_Actors.Count; i++)
            {
                array[i] = m_Actors[i];
            }
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void CopyToWithTargetPriority(IStageActor[] array)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            for (int i = 0; i < m_Actors.Count; i++)
            {
                array[i] = m_Actors[i];
            }
            Array.Sort(array, TargetingPriorityComparer.Static);
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public void CopyTo(IStageActor[] array, int arrayIndex)
        {
            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            for (int i = arrayIndex, s = 0;
                 i < array.Length && s < m_Actors.Count;
                 i++, s++)
            {
                array[i] = m_Actors[s];
            }
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public IEnumerator<IStageActor> GetEnumerator()
        {
            var array = new IStageActor[Count];
            using (var wl = new SemaphoreSlimLock(m_Lock))
            {
                wl.Wait(TimeSpan.FromSeconds(1));

                m_Actors.CopyTo(array);
            }

            for (int i = 0; i < array.Length; i++)
            {
                yield return (array[i]);
            }
            Array.Clear(array, 0, array.Length);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [ThreadSafe(ThreadSafeAttribute.SafeType.Semaphore)]
        public bool ResolvePosition(IStageActor runtimeActor)
        {
            if (runtimeActor.OverrideFront) return true;

            using var wl = new SemaphoreSlimLock(m_Lock);
            wl.Wait(TimeSpan.FromSeconds(1));

            int count = Count;
            // If no or just one actor in the field, always front
            if (count <= 1) return true;

            // This because field list is ordered list by ActorPositionComparer.
            // If the first element is defensive(2), should direct comparison with given actor
            if (m_Actors[0].Data.Type == ActorSheet.ActorType.Defensive)
            {
                int order = ActorPositionComparer.Static.Compare(runtimeActor, m_Actors[0]);
                // If only actor is Offensive(0)
                if (order < 0) return false;
                // If actor also defensive(2)
                if (order == 0)
                {
                    return true;
                }

                return false;
            }

            var lastActor = m_Actors[^1];
            // If first is not defensive, should iterate all fields until higher order found
            for (int i = 0; i < count; i++)
            {
                IStageActor e     = m_Actors[i];
                int         order = ActorPositionComparer.Static.Compare(runtimeActor, e);
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