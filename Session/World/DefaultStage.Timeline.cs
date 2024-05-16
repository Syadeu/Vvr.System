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
    partial class DefaultStage : IConnector<ITimelineProvider>
    {
        private readonly ActorList         m_Timeline      = new();
        private          ITimelineProvider m_TimelineProvider;

        private partial void DequeueTimeline()
        {
            if (m_Timeline.Count > 0) m_Timeline.RemoveAt(0);

            UpdateTimeline();
        }
        private partial void UpdateTimeline()
        {
            const int maxTimelineCount = 5;

            if (m_Timeline.Count > 0 && !m_TimelineProvider.IsStartFrom(m_Timeline[0]))
            {
                m_TimelineProvider.StartFrom(m_Timeline[0]);
                for (int i = m_Timeline.Count - 1; i >= 1; i--)
                {
                    m_Timeline.RemoveAt(i);
                }
            }

            while (m_TimelineProvider.Count > 0 && m_Timeline.Count < maxTimelineCount)
            {
                m_Timeline.Add((StageActor)m_TimelineProvider.Dequeue());
            }
        }

        private partial async UniTask Join(ActorList field, StageActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor, ActorPositionComparer.Static);

            var viewProvider = await m_ViewProvider;
            var view         = await viewProvider.Resolve(actor.owner);

            bool    isFront = ResolvePosition(field, actor);
            Vector3 pos     = view.localPosition;
            pos.z              = isFront ? 1 : 0;
            view.localPosition = pos;

            m_TimelineProvider.Enqueue(actor);
        }
        private partial async UniTask JoinAfter(StageActor target, ActorList field, StageActor actor)
        {
            Assert.IsFalse(field.Contains(actor));
            field.Add(actor, ActorPositionComparer.Static);

            int index = m_TimelineProvider.IndexOf(target);
            m_TimelineProvider.InsertAfter(
                index, actor);

            using (var trigger = ConditionTrigger.Push(actor.owner, ConditionTrigger.Game))
            {
                await trigger.Execute(Model.Condition.OnTagIn, null);
            }
        }

        private partial async UniTask Delete(ActorList field, StageActor actor)
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
        private partial async UniTask RemoveFromQueue(StageActor actor)
        {
            m_TimelineProvider.Remove(actor);
        }
        private partial async UniTask RemoveFromTimeline(StageActor actor, int preserveCount = 0)
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
        private bool ResolvePosition(IList<StageActor> field, IStageActor runtimeActor)
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
                StageActor e    = field[i];
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

        void IConnector<ITimelineProvider>.Connect(ITimelineProvider t)
        {
            Assert.IsNull(m_TimelineProvider);
            m_TimelineProvider = t;
        }

        void IConnector<ITimelineProvider>.Disconnect(ITimelineProvider t)
        {
            Assert.IsTrue(ReferenceEquals(m_TimelineProvider, t));
            m_TimelineProvider = null;
        }
    }
}