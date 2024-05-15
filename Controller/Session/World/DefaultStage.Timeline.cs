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
using Vvr.Controller.Actor;
using Vvr.Controller.CustomMethod;
using Vvr.Model;
using Vvr.Model.Stat;

namespace Vvr.Controller.Session.World
{
    partial class DefaultStage
    {
        struct TimelineActor
        {
            public IActor actor;
            public float  time;
        }

        // private readonly LinkedList<TimelineActor> m_Timeline = new();

        private void CycleTimeline()
        {

        }
        private partial async UniTask Join(ActorList field, RuntimeActor actor)
        {
            field.Add(actor, ActorPositionComparer.Static);

            var viewProvider = await m_ViewProvider;
            var view         = await viewProvider.Resolve(actor.owner);

            bool    isFront = ResolvePosition(field, actor);
            Vector3 pos     = view.localPosition;
            pos.z              = isFront ? 1 : 0;
            view.localPosition = pos;

            m_Queue.Enqueue(
                actor,
                CustomMethodProvider.Static.Resolve(actor.owner.Stats, "TIMELINE")
                // actor.owner.Stats[StatType.SPD]
                );
        }

        private partial async UniTask Delete(ActorList field, RuntimeActor actor)
        {
            bool result = field.Remove(actor);
            Assert.IsTrue(result);

            m_Queue.RemoveAll(e => e.owner == actor.owner);
            // LinkedListNode<TimelineActor> current = m_Timeline.First;
            // while (current != null)
            // {
            //     LinkedListNode<TimelineActor> next = current.Next;
            //     if (current.Value.actor == actor.owner)
            //     {
            //         m_Timeline.Remove(current);
            //     }
            //     current = next;
            // }
            for (int i = 0; i < m_Timeline.Count; i++)
            {
                var e = m_Timeline[i];
                if (e.owner != actor.owner) continue;

                m_Timeline.RemoveAt(i);
                i--;
            }

            var viewProvider = await m_ViewProvider;
            await viewProvider.Release(actor.owner);
            actor.owner.Release();
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