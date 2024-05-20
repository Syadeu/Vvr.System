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

using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a session used for managing game data.
    /// </summary>
    [UsedImplicitly]
    public sealed class GameDataSession : ParentSession<GameDataSession.SessionData>
    {
        public struct SessionData : ISessionData
        {

        }

        public override string DisplayName => nameof(GameDataSession);

        [PublicAPI]
        public GameDataSheets SheetContainer { get; private set; }

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            var assetSession = await CreateSession<AssetSession>(
                new AssetSession.SessionData(
                    "GameData"
                    ));
            Register<IAssetProvider>(assetSession);

            SheetContainer = new GameDataSheets(UnityLogger.Default);
            var dataContainer
                = await assetSession.LoadAsync<SheetContainerScriptableObject>("Data/_Container.asset");

            ScriptableObjectSheetImporter imp = new(dataContainer.Object);
            await SheetContainer.Bake(imp).AsUniTask();

            StatProvider.GetOrCreate(SheetContainer.StatTable);

            var gameConfigSession = await CreateSession<GameConfigSession>(
                new GameConfigSession.SessionData(SheetContainer.GameConfigTable));
            var actorDataSession = await CreateSession<ActorDataSession>(
                new ActorDataSession.SessionData(SheetContainer.Actors));
            var customMethodSession = await CreateSession<CustomMethodSession>(
                new CustomMethodSession.SessionData(SheetContainer.CustomMethodTable));
            var stageDataSession = await CreateSession<StageDataSession>(
                new StageDataSession.SessionData(SheetContainer.Stages));

            Parent
                .Register<IGameConfigProvider>(gameConfigSession)
                .Register<IActorDataProvider>(actorDataSession)
                .Register<ICustomMethodProvider>(customMethodSession)
                .Register<IStageDataProvider>(stageDataSession)
                ;
        }
        protected override UniTask OnReserve()
        {
            Parent
                .Unregister<IGameConfigProvider>()
                .Unregister<IActorDataProvider>()
                .Unregister<ICustomMethodProvider>()
                .Unregister<IStageDataProvider>()
                ;

            SheetContainer.Dispose();

            return base.OnReserve();
        }
    }
}