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
// File created : 2024, 05, 17 03:05

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Vvr.Controller.Actor;
using Vvr.Controller.Condition;
using Vvr.Controller.Provider;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Actor;
using Vvr.Session.EventView.Core;
using Vvr.Session.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class StageActorFactorySession : ChildSession<StageActorFactorySession.SessionData>,
        IStageActorProvider,
        IConnector<IResearchDataProvider>,
        IConnector<IEffectViewProvider>,
        IConnector<IGameObjectPoolViewProvider>,
        IConnector<ICanvasViewProvider>,
        IConnector<IActorViewProvider>
    {
        public struct SessionData : ISessionData
        {
        }

        private sealed class StageActor : IStageActor,
            IConnector<IAssetProvider>,
            IConnector<IActorViewProvider>,
            IConnector<ITargetProvider>,
            IConnector<IActorDataProvider>,
            IConnector<IEventConditionProvider>,
            IConnector<IStateConditionProvider>,

            IConnector<IEffectViewProvider>
        {
            private readonly IActor     m_Owner;
            private readonly IActorData m_Data;

            private readonly IDynamicConditionObserver m_Observer;

            private bool
                m_IsTurnEnd,
                m_TagOutRequested,
                m_OverrideFront;

            private int m_TargetingPriority;

            private int m_ParryCount;

            public IActor Owner
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_Owner;
                }
            }

            public IActorData Data
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_Data;
                }
            }
            public bool IsTurnEnd
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_IsTurnEnd;
                }
            }

            public bool TagOutRequested
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_TagOutRequested;
                }
                set
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    m_TagOutRequested = value;
                }
            }

            public bool OverrideFront
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_OverrideFront;
                }
                set
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    m_OverrideFront = value;
                }
            }

            public int TargetingPriority
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_TargetingPriority;
                }
                set
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    m_TargetingPriority = value;
                }
            }

            public int ParryCount
            {
                get
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    return m_ParryCount;
                }
                set
                {
                    if (Disposed)
                        throw new ObjectDisposedException(nameof(StageActor));
                    m_ParryCount = value;
                }
            }

            public bool Disposed { get; private set; }

            public StageActor(IActor o, IActorData d, IDynamicConditionObserver observer)
            {
                m_Owner = o;
                m_Data  = d;

                m_Observer = observer;

                m_Observer[Condition.OnActorTurn] += OnActorTurn;
                m_Observer[Condition.OnActorTurnEnd] += OnActorTurnEnd;
            }

            private UniTask OnActorTurn(IEventTarget owner, string value, CancellationToken cancellationtoken)
            {
                m_IsTurnEnd = false;
                return UniTask.CompletedTask;
            }
            private UniTask OnActorTurnEnd(IEventTarget owner, string value, CancellationToken cancellationtoken)
            {
                m_IsTurnEnd = true;
                return UniTask.CompletedTask;
            }

            public void Dispose()
            {
                m_Observer.Dispose();
                Disposed = true;
            }

            void IConnector<IAssetProvider>.Connect(IAssetProvider t)
            {
                Owner.Assets.Connect(t);
            }

            void IConnector<IAssetProvider>.Disconnect(IAssetProvider t)
            {
                Owner.Assets.Disconnect(t);
            }

            void IConnector<ITargetProvider>.Connect(ITargetProvider t)
            {
                Owner.Skill.Connect(t);
                Owner.Passive.Connect(t);
            }

            void IConnector<ITargetProvider>.Disconnect(ITargetProvider t)
            {
                Owner.Skill.Disconnect(t);
                Owner.Passive.Disconnect(t);
            }

            void IConnector<IActorDataProvider>.Connect(IActorDataProvider t)
            {
                Owner.Skill.Connect(t);
            }

            void IConnector<IActorDataProvider>.Disconnect(IActorDataProvider t)
            {
                Owner.Skill.Disconnect(t);
            }

            void IConnector<IEventConditionProvider>.Connect(IEventConditionProvider t)
            {
                Owner.ConditionResolver.Connect(t);
            }

            void IConnector<IEventConditionProvider>.Disconnect(IEventConditionProvider t)
            {
                Owner.ConditionResolver.Disconnect(t);
            }

            void IConnector<IStateConditionProvider>.Connect(IStateConditionProvider t)
            {
                Owner.ConditionResolver.Connect(t);
            }

            void IConnector<IStateConditionProvider>.Disconnect(IStateConditionProvider t)
            {
                Owner.ConditionResolver.Disconnect(t);
            }

            void IConnector<IActorViewProvider>.Connect(IActorViewProvider t)
            {
                Owner.Skill.Connect(t);
            }
            void IConnector<IActorViewProvider>.Disconnect(IActorViewProvider t)
            {
                Owner.Skill.Disconnect(t);
            }

            void IConnector<IEffectViewProvider>.Connect(IEffectViewProvider t)
            {
                Owner.Skill.Connect(t);
            }
            void IConnector<IEffectViewProvider>.Disconnect(IEffectViewProvider t)
            {
                Owner.Skill.Disconnect(t);
            }
        }
        private struct ActorComparer : IComparer<StageActor>, IComparer<IActor>
        {
            public static readonly Func<StageActor, IActor> ActorSelector = x => x.Owner;
            public static readonly IComparer<StageActor>        StageActorStatic   = default(ActorComparer);
            public static readonly IComparer<IActor>        ActorStatic   = default(ActorComparer);

            int IComparer<StageActor>.Compare(StageActor x, StageActor y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                int xx = x.Owner.GetInstanceID(),
                    yy = y.Owner.GetInstanceID();

                return xx.CompareTo(yy);
            }

            public int Compare(IActor x, IActor y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                int xx = x.GetInstanceID(),
                    yy = y.GetInstanceID();

                return xx.CompareTo(yy);
            }
        }

        private readonly List<StageActor> m_Created = new();

        private IResearchDataProvider       m_ResearchDataProvider;
        private IGameObjectPoolViewProvider m_GameObjectPoolViewProvider;

        private ICanvasViewProvider m_CanvasViewProvider;
        private IActorViewProvider  m_ActorViewProvider;

        private SpinLock m_SpinLock = new();

        public override string DisplayName => nameof(StageActorFactorySession);

        protected override async UniTask OnReserve()
        {
            await base.OnReserve();

            Clear();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public IStageActor Get(IActor actor)
        {
            Assert.IsNotNull(actor);

            using var timer = DebugTimer.StartWithCustomName(
                DebugTimer.BuildDisplayName(nameof(StageActorFactorySession), nameof(Get))
            );

            bool wl = false;
            try
            {
                m_SpinLock.Enter(ref wl);
                return m_Created.BinarySearch(ActorComparer.ActorSelector, ActorComparer.ActorStatic, actor);
            }
            finally
            {
                if (wl)
                    m_SpinLock.Exit();
            }
        }
        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public IStageActor Create(IActor actor, IActorData data)
        {
            Assert.IsNotNull(actor);
            Assert.IsNotNull(data);
            using var timer = DebugTimer.StartWithCustomName(
                DebugTimer.BuildDisplayName(nameof(StageActorFactorySession), nameof(Create))
                );

            StageActor result;
            bool       wl = false;
            try
            {
                m_SpinLock.Enter(ref wl);

                result = m_Created.BinarySearch(ActorComparer.ActorSelector, ActorComparer.ActorStatic, actor);
                if (result is null)
                {
                    var ob = actor.ConditionResolver.CreateObserver();
                    SetupObserver(ob);

                    result = new StageActor(actor, data, ob);

                    IActor item = result.Owner;
                    Connect<IAssetProvider>(result)
                        .Connect<ITargetProvider>(result)
                        .Connect<IActorViewProvider>(result)
                        .Connect<IActorDataProvider>(result)
                        .Connect<IEventConditionProvider>(result)
                        .Connect<IStateConditionProvider>(result)
                        .Connect<IEffectViewProvider>(result)
                        ;

                    item.ConnectTime();

                    // Only if player actor
                    if (actor.Owner == Owner)
                    {
                        foreach (var nodeGroup in m_ResearchDataProvider)
                        {
                            foreach (var node in nodeGroup)
                            {
                                item.Stats.AddModifier(node);
                            }
                        }
                    }

                    m_Created.Add(result, ActorComparer.StageActorStatic);
                }
            }
            finally
            {
                if (wl)
                    m_SpinLock.Exit();
            }
            return result;
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public void Reserve(IStageActor item)
        {
            Assert.IsNotNull(item);
            using var timer = DebugTimer.StartWithCustomName(
                DebugTimer.BuildDisplayName(nameof(StageActorFactorySession), nameof(Reserve))
                );

            StageActor actor;
            bool       wl = false;
            try
            {
                m_SpinLock.Enter(ref wl);

                if (item is not StageActor stageActor ||
                    !m_Created.Remove(stageActor, ActorComparer.StageActorStatic))
                    throw new InvalidOperationException();

                actor = stageActor;
            }
            finally
            {
                if (wl)
                    m_SpinLock.Exit();
            }

            DisconnectActor(actor);
            item.Dispose();
        }

        [ThreadSafe(ThreadSafeAttribute.SafeType.SpinLock)]
        public void Clear()
        {
            using var timer = DebugTimer.StartWithCustomName(
                DebugTimer.BuildDisplayName(nameof(StageActorFactorySession), nameof(Clear))
            );

            bool wl = false;
            try
            {
                m_SpinLock.Enter(ref wl);

                for (int i = m_Created.Count - 1; i >= 0; i--)
                {
                    var e = m_Created[i];
                    DisconnectActor(e);
                    e.Dispose();
                }

                m_Created.Clear();
            }
            finally
            {
                if (wl)
                    m_SpinLock.Exit();
            }
        }

        private void DisconnectActor(StageActor item)
        {
            using var timer = DebugTimer.Start();

            // Only if player actor
            if (item.Owner.Owner == Owner)
            {
                foreach (var nodeGroup in m_ResearchDataProvider)
                {
                    foreach (var node in nodeGroup)
                    {
                        item.Owner.Stats.RemoveModifier(node);
                    }
                }
            }

            Disconnect<IAssetProvider>(item)
                .Disconnect<ITargetProvider>(item)
                .Disconnect<IActorViewProvider>(item)
                .Disconnect<IActorDataProvider>(item)
                .Disconnect<IEventConditionProvider>(item)
                .Disconnect<IStateConditionProvider>(item)
                .Disconnect<IEffectViewProvider>(item)
                ;

            item.Owner.DisconnectTime();
            item.Owner.Skill.Clear();
            // item.Owner.Passive.cle
            item.Owner.Abnormal.Clear();
        }

        private void SetupObserver(IDynamicConditionObserver observer)
        {
            observer[Condition.OnHit] += async (owner, value, token) =>
            {
                var    stageActor = Get((IActor)owner);
                Assert.IsNotNull(stageActor);

                if (stageActor.ParryCount > 0)
                {
                    stageActor.ParryCount--;
                    var view = await m_ActorViewProvider.ResolveAsync(owner);

                    using var trigger = ConditionTrigger.Push(owner);
                    await trigger.Execute(Condition.OnParrying, null, token);

                    var canvas = m_CanvasViewProvider.ResolveCamera(
                        CanvasCameraType.Default, RenderMode.WorldSpace, CanvasLayerName.OverlayUI, 0, false);

                    using (var scope = await m_GameObjectPoolViewProvider.Scope("Contents/Effect/Parrying Effect.prefab", token))
                    {
                        scope.Object.transform.SetParent(canvas.Object.transform, false);

                        Vector3 pos = view.position + new Vector3(25, 0);
                        pos.y += UnityEngine.Random.Range(-10, 25);

                        scope.Object.transform.position = pos;

                        scope.Object.SetActive(true);

                        await UniTask.WaitForSeconds(1);

                        scope.Object.SetActive(false);
                    }
                }
            };
        }

        void IConnector<IResearchDataProvider>.Connect(IResearchDataProvider    t) => m_ResearchDataProvider = t;
        void IConnector<IResearchDataProvider>.Disconnect(IResearchDataProvider t) => m_ResearchDataProvider = null;

        void IConnector<IEffectViewProvider>.Connect(IEffectViewProvider t)
        {

        }
        void IConnector<IEffectViewProvider>.Disconnect(IEffectViewProvider t)
        {
        }

        void IConnector<IGameObjectPoolViewProvider>.Connect(IGameObjectPoolViewProvider    t) => m_GameObjectPoolViewProvider = t;
        void IConnector<IGameObjectPoolViewProvider>.Disconnect(IGameObjectPoolViewProvider t) => m_GameObjectPoolViewProvider = null;

        void IConnector<IActorViewProvider>.Connect(IActorViewProvider t) => m_ActorViewProvider = t;
        void IConnector<IActorViewProvider>.Disconnect(IActorViewProvider t) => m_ActorViewProvider = null;

        void IConnector<ICanvasViewProvider>.Connect(ICanvasViewProvider    t) => m_CanvasViewProvider = t;
        void IConnector<ICanvasViewProvider>.Disconnect(ICanvasViewProvider t) => m_CanvasViewProvider = null;
    }
}