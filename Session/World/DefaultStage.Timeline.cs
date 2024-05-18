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

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Condition;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.Provider;

namespace Vvr.Session.World
{
    partial class DefaultStage : IConnector<ITimelineQueueProvider>
    {
        private readonly ActorList         m_Timeline      = new();
        private          ITimelineQueueProvider m_TimelineQueueProvider;

        /// <summary>
        /// Dequeues the first actor from the timeline and updates it.
        /// </summary>
        private partial void DequeueTimeline()
        {
            if (m_Timeline.Count > 0) m_Timeline.RemoveAt(0);

            UpdateTimeline();
        }

        /// <summary>
        /// Updates the timeline by checking the current state and adding or removing actors as necessary.
        /// </summary>
        private partial void UpdateTimeline()
        {
            const int maxTimelineCount = 5;

            if (m_Timeline.Count > 0 && !m_TimelineQueueProvider.IsStartFrom(m_Timeline[0]))
            {
                m_TimelineQueueProvider.StartFrom(m_Timeline[0]);
                for (int i = m_Timeline.Count - 1; i >= 1; i--)
                {
                    m_Timeline.RemoveAt(i);
                }
            }

            while (m_TimelineQueueProvider.Count > 0 && m_Timeline.Count < maxTimelineCount)
            {
                m_Timeline.Add(m_TimelineQueueProvider.Dequeue());
            }
        }

        /// <summary>
        /// Joins an actor to the specified field in the stage.
        /// </summary>
        /// <param name="field">The field in the stage where the actor will be joined.</param>
        /// <param name="actor">The actor to be joined.</param>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        private partial async UniTask Join(ActorList field, IStageActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor, ActorPositionComparer.Static);

            var view = await m_ViewProvider.CardViewProvider.Resolve(actor.Owner);

            bool    isFront = ResolvePosition(field, actor);
            Vector3 pos     = view.localPosition;
            pos.z              = isFront ? 1 : 0;
            view.localPosition = pos;

            m_TimelineQueueProvider.Enqueue(actor);
        }

        /// <summary>
        /// Adds an actor to the specified field after a target actor in the timeline queue.
        /// </summary>
        /// <param name="target">The target actor after which the actor should be added.</param>
        /// <param name="field">The field to which the actor should be added.</param>
        /// <param name="actor">The actor to be added.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        private partial async UniTask JoinAfter(IStageActor target, ActorList field, IStageActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor, ActorPositionComparer.Static);

            int index = m_TimelineQueueProvider.IndexOf(target);
            m_TimelineQueueProvider.InsertAfter(
                index, actor);
        }

        /// <summary>
        /// Deletes an actor from the actor list and performs necessary cleanup operations.
        /// </summary>
        /// <param name="field">The actor list from which to delete the actor.</param>
        /// <param name="actor">The actor to delete.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        private partial async UniTask Delete(ActorList field, IStageActor actor)
        {
            bool result = field.Remove(actor);
            Assert.IsTrue(result);

            await RemoveFromTimeline(actor);
            await RemoveFromQueue(actor);

            await m_ViewProvider.CardViewProvider.Release(actor.Owner);
            actor.Owner.Release();

            UpdateTimeline();
        }

        /// <summary>
        /// Removes the specified actor from the queue.
        /// </summary>
        /// <param name="actor">The actor to remove from the queue.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous removal of the actor from the queue.</returns>
        private partial async UniTask RemoveFromQueue(IStageActor actor)
        {
            m_TimelineQueueProvider.Remove(actor);
        }

        /// <summary>
        /// Removes the specified actor from the timeline.
        /// </summary>
        /// <param name="actor">The actor to be removed from the timeline.</param>
        /// <param name="preserveCount">The number of actors to preserve after removing the specified actor from the timeline. Default is 0.</param>
        /// <returns>A <see cref="UniTask"/> representing the asynchronous operation.</returns>
        private partial async UniTask RemoveFromTimeline(IStageActor actor, int preserveCount = 0)
        {
            for (int i = 0; i < m_Timeline.Count; i++)
            {
                var e = m_Timeline[i];
                if (e.Owner != actor.Owner) continue;

                if (0 < preserveCount--) continue;

                m_Timeline.RemoveAt(i);
                i--;
            }
        }

        [MustUseReturnValue]
        private bool ResolvePosition(IList<IStageActor> field, IStageActor runtimeActor)
        {
            int count = field.Count;
            // If no actor in the field, always front
            if (count == 0) return true;

            // This because field list is ordered list by ActorPositionComparer.
            // If the first element is defensive(2), should direct comparison with given actor
            if (field[0].Data.Type == ActorSheet.ActorType.Defensive)
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
                IStageActor e    = field[i];
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
    }
}