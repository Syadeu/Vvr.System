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
// File created : 2024, 05, 23 20:05

#endregion

using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Controller.Research;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session.AssetManagement
{
    [UsedImplicitly]
    [ParentSession(typeof(GameDataSession))]
    public sealed class ResearchDataSession : ChildSession<ResearchDataSession.SessionData>,
        IResearchDataProvider,
        IConnector<IUserDataProvider>,
        IConnector<IStatConditionProvider>
    {
        public struct SessionData : ISessionData
        {
            public readonly ResearchSheet sheet;

            public SessionData(ResearchSheet s)
            {
                sheet = s;
            }
        }

        public override string DisplayName => nameof(ResearchDataSession);

        private IStatConditionProvider m_StatConditionProvider;

        // TODO: probably change int key to enum?
        private readonly Dictionary<int, IResearchNodeGroup> m_NodeGroups = new();

        public IResearchNodeGroup this[int i] => m_NodeGroups[i];

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            // Assert.IsNotNull(m_StatConditionProvider);
            //
            // Dictionary<int, LinkedList<ResearchSheet.Row>> dataMap = new();
            // foreach (var e in data.sheet)
            // {
            //     if (!dataMap.TryGetValue(e.Definition.Group, out var list))
            //     {
            //         list                        = new();
            //         dataMap[e.Definition.Group] = list;
            //     }
            //
            //     list.AddLast(e);
            // }
            //
            // foreach (var item in dataMap)
            // {
            //     ResearchNodeGroup group = ResearchNodeGroup.Build(
            //         m_StatConditionProvider,
            //         item.Value);
            //     m_NodeGroups[item.Key] = group;
            // }
        }

        protected override async UniTask OnReserve()
        {
            m_NodeGroups.Clear();

            await base.OnReserve();
        }

        public IEnumerator<IResearchNodeGroup> GetEnumerator()
        {
            foreach (var nodeGroup in m_NodeGroups.Values)
            {
                yield return nodeGroup;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IConnector<IUserDataProvider>.Connect(IUserDataProvider t)
        {
            // Because data session always initialized before user session.
            // So we can safely initialize all nodes
            foreach (var nodeGroup in m_NodeGroups.Values)
            {
                foreach (var node in nodeGroup)
                {
                    node.SetLevel(t.GetInt(UserDataPath.Research.NodeLevel(node.Id)));
                }
            }
        }
        void IConnector<IUserDataProvider>.Disconnect(IUserDataProvider t)
        {
        }

        void IConnector<IStatConditionProvider>.Connect(IStatConditionProvider    t)
        {
            m_StatConditionProvider = t;

            Dictionary<int, LinkedList<ResearchSheet.Row>> dataMap = new();
            foreach (var e in Data.sheet)
            {
                if (!dataMap.TryGetValue(e.Definition.Group, out var list))
                {
                    list                        = new();
                    dataMap[e.Definition.Group] = list;
                }

                list.AddLast(e);
            }

            foreach (var item in dataMap)
            {
                ResearchNodeGroup group = ResearchNodeGroup.Build(
                    m_StatConditionProvider,
                    item.Value);
                m_NodeGroups[item.Key] = group;
            }
        }

        void IConnector<IStatConditionProvider>.Disconnect(IStatConditionProvider t) => m_StatConditionProvider = null;
    }
}