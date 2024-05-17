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
// File created : 2024, 05, 17 13:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    [UsedImplicitly]
    public sealed class GameDataSession : ParentSession<GameDataSession.SessionData>
    {
        public struct SessionData : ISessionData
        {

        }

        private GameDataSheets m_SheetContainer;

        public override string DisplayName => nameof(GameDataSession);

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var assetSession = await CreateSession<AssetSession>(
                new AssetSession.SessionData(
                    "GameData"
                    ));
            Register<IAssetProvider>(assetSession);

            m_SheetContainer = new GameDataSheets(UnityLogger.Default);
            var dataContainer
                = await assetSession.LoadAsync<SheetContainerScriptableObject>("Data/_Container.asset");

            ScriptableObjectSheetImporter imp = new(dataContainer.Object);
            await m_SheetContainer.Bake(imp).AsUniTask();

            var gameConfigSession = await CreateSession<GameConfigSession>(
                new GameConfigSession.SessionData(m_SheetContainer.GameConfigTable));
            Parent.Register<IGameConfigProvider>(gameConfigSession);
        }
    }

    [UsedImplicitly]
    [ParentSession(typeof(GameDataSession))]
    public class GameConfigSession : ChildSession<GameConfigSession.SessionData>,
        IGameConfigProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly GameConfigSheet sheet;

            public SessionData(GameConfigSheet s)
            {
                sheet = s;
            }
        }

        private readonly Dictionary<MapType, LinkedList<GameConfigSheet.Row>> m_Configs = new();

        public override string DisplayName => nameof(GameConfigSession);

        public IEnumerable<GameConfigSheet.Row> this[MapType t]
        {
            get
            {
                if (!m_Configs.TryGetValue(t, out var list))
                {
                    return Array.Empty<GameConfigSheet.Row>();
                }

                return list;
            }
        }

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            foreach (var item in data.sheet)
            {
                if (!m_Configs.TryGetValue(item.Lifecycle.Map, out var list))
                {
                    list                          = new();
                    m_Configs[item.Lifecycle.Map] = list;
                }

                list.AddLast(item);
            }

            return base.OnInitialize(session, data);
        }

        protected override UniTask OnReserve()
        {
            foreach (var list in m_Configs.Values)
            {
                list.Clear();
            }

            m_Configs.Clear();
            return base.OnReserve();
        }
    }
}