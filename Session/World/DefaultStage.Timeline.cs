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

using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
    partial class DefaultStage :
        IConnector<ITimelineQueueProvider>,
        IConnector<IEventTimelineNodeViewProvider>
    {
        private readonly List<IStageActor> m_Timeline = new();
        private readonly List<float>       m_Times    = new();

        private ITimelineQueueProvider     m_TimelineQueueProvider;
        private IEventTimelineNodeViewProvider m_TimelineNodeViewProvider;

        /// <summary>
        /// Dequeues the first actor from the timeline and updates it.
        /// </summary>
        private partial float DequeueTimeline()
        {
            float t = 0;
            if (m_Timeline.Count > 0)
            {
                t = m_Times[0];
                m_Timeline.RemoveAt(0);
                m_Times.RemoveAt(0);
            }

            UpdateTimeline();
            return t;
        }

        /// <summary>
        /// Updates the timeline by checking the current state and adding or removing actors as necessary.
        /// </summary>
        private partial void UpdateTimeline()
        {
            const int maxTimelineCount = 15;

            if (m_Timeline.Count > 0 && !m_TimelineQueueProvider.IsStartFrom(m_Timeline[0]))
            {
                m_TimelineQueueProvider.StartFrom(m_Timeline[0]);
                for (int i = m_Timeline.Count - 1; i >= 1; i--)
                {
                    m_Timeline.RemoveAt(i);
                    m_Times.RemoveAt(i);
                }
            }

            if (!m_TimelineQueueProvider.HasAnyEnabled) return;

            while (m_Timeline.Count < maxTimelineCount)
            {
                var next = m_TimelineQueueProvider.Dequeue(out float time);
                if (next is null) break;

                m_Timeline.Add(next);
                m_Times.Add(time);
            }
        }

        private int FindTimelineIndex(IStageActor x)
        {
            for (int i = 0; i < m_Timeline.Count; i++)
            {
                var e = m_Timeline[i];
                if (ReferenceEquals(e.Owner, x.Owner)) return i;
            }

            return -1;
        }
        private partial async UniTask UpdateTimelineNodeViewAsync(CancellationToken cancellationToken)
        {
            int       count = m_PlayerField.Count + m_EnemyField.Count;
            UniTask[] tasks = ArrayPool<UniTask>.Shared.Rent(count);
            int       i     = 0;
            for (; i < m_PlayerField.Count; i++)
            {
                var e     = m_PlayerField[i];
                int index = FindTimelineIndex(e);
                tasks[i] = m_TimelineNodeViewProvider.ResolveAsync(e.Owner, index)
                    .AttachExternalCancellation(cancellationToken);
            }
            for (int j = 0; j < m_EnemyField.Count; j++, i++)
            {
                var e     = m_EnemyField[j];
                int index = FindTimelineIndex(e);
                tasks[i] = m_TimelineNodeViewProvider.ResolveAsync(e.Owner, index)
                    .AttachExternalCancellation(cancellationToken);
            }

            await UniTask.WhenAll(tasks)
                .AttachExternalCancellation(cancellationToken);
            ArrayPool<UniTask>.Shared.Return(tasks, true);
        }
        private partial async UniTask CloseTimelineNodeViewAsync(CancellationToken cancellationToken = default)
        {
            int       count = m_PlayerField.Count + m_EnemyField.Count;
            UniTask[] tasks = ArrayPool<UniTask>.Shared.Rent(count);
            int       i     = 0;
            for (; i < m_PlayerField.Count; i++)
            {
                tasks[i] = m_TimelineNodeViewProvider.Release(m_PlayerField[i].Owner)
                    .AttachExternalCancellation(cancellationToken);
            }
            for (int j = 0; j < m_EnemyField.Count; j++, i++)
            {
                tasks[i] = m_TimelineNodeViewProvider.Release(m_EnemyField[j].Owner)
                    .AttachExternalCancellation(cancellationToken);
            }

            await UniTask.WhenAll(tasks)
                .AttachExternalCancellation(cancellationToken);
            ArrayPool<UniTask>.Shared.Return(tasks, true);
        }

        /// <summary>
        /// Joins an actor to the specified field in the stage.
        /// </summary>
        /// <param name="field">The field in the stage where the actor will be joined.</param>
        /// <param name="actor">The actor to be joined.</param>
        private partial void Join(IStageActorField field, IStageActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor);

            m_TimelineQueueProvider.Enqueue(actor);

            foreach (var e in field)
            {
                m_ViewProvider.ResolveAsync(e.Owner)
                    .AttachExternalCancellation(ReserveToken)
                    .SuppressCancellationThrow()
                    .Forget()
                    ;
            }
        }

        /// <summary>
        /// Adds an actor to the specified field after a target actor in the timeline queue.
        /// </summary>
        /// <param name="target">The target actor after which the actor should be added.</param>
        /// <param name="field">The field to which the actor should be added.</param>
        /// <param name="actor">The actor to be added.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        private partial void JoinAfter(IStageActor target, IStageActorField field, IStageActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor);

            int index = m_TimelineQueueProvider.IndexOf(target);
            m_TimelineQueueProvider.InsertAfter(
                index, actor);
        }

        /// <summary>
        /// Deletes an actor from the actor list and performs necessary cleanup operations.
        /// </summary>
        /// <param name="field">The actor list from which to delete the actor.</param>
        /// <param name="stageActor">The actor to delete.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        private partial async UniTask DeleteAsync(IList<IStageActor> field, IStageActor stageActor)
        {
            bool result = field.Remove(stageActor);
            Assert.IsTrue(result);

            RemoveFromTimeline(stageActor);
            RemoveFromQueue(stageActor);

            await m_TimelineNodeViewProvider.Release(stageActor.Owner);
            await m_ViewProvider.ReleaseAsync(stageActor.Owner);
            // actor.Owner.Release();

            var actor = stageActor.Owner;
            m_StageActorProvider.Reserve(stageActor);
            actor.Release();
            // DelayedDelete(actor).Forget();

            UpdateTimeline();
        }

        /// <summary>
        /// Removes the specified actor from the queue.
        /// </summary>
        /// <param name="actor">The actor to remove from the queue.</param>
        private partial void RemoveFromQueue(IStageActor actor)
        {
            m_TimelineQueueProvider.Remove(actor);
        }

        /// <summary>
        /// Removes the specified actor from the timeline.
        /// </summary>
        /// <param name="actor">The actor to be removed from the timeline.</param>
        /// <param name="preserveCount">The number of actors to preserve after removing the specified actor from the timeline. Default is 0.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        private partial void RemoveFromTimeline(IStageActor actor, int preserveCount)
        {
            for (int i = 0; i < m_Timeline.Count; i++)
            {
                var e = m_Timeline[i];
                if (e.Owner != actor.Owner) continue;

                if (0 < preserveCount--) continue;

                m_Timeline.RemoveAt(i);
                m_Times.RemoveAt(i);
                i--;
            }
        }

        void IConnector<ITimelineQueueProvider>.Connect(ITimelineQueueProvider t)
        {
            Assert.IsNull(m_TimelineQueueProvider);
            m_TimelineQueueProvider = t;
        }
        void IConnector<ITimelineQueueProvider>.Disconnect(ITimelineQueueProvider t)
        {
            Assert.IsTrue(ReferenceEquals(m_TimelineQueueProvider, t));
            m_TimelineQueueProvider = null;
        }
        void IConnector<IEventTimelineNodeViewProvider>.Connect(IEventTimelineNodeViewProvider    t) => m_TimelineNodeViewProvider = t;
        void IConnector<IEventTimelineNodeViewProvider>.Disconnect(IEventTimelineNodeViewProvider t) => m_TimelineNodeViewProvider = null;
    }
}