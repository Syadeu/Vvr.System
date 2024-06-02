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
using Vvr.Session.Provider;

namespace Vvr.Session.AssetManagement
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

            const string DATA_KEY = "Data/_Container.asset";

            var assetSession = await CreateSession<AssetSession>(
                new AssetSession.SessionData(
                    // "GameData"
                ));
            Register<IAssetProvider>(assetSession);

            await UniTask.SwitchToMainThread();
            SheetContainer = new GameDataSheets(UnityLogger.Default);
            IImmutableObject<SheetContainerScriptableObject> dataContainer
                = await assetSession.LoadAsync<SheetContainerScriptableObject>(DATA_KEY);

            ScriptableObjectSheetImporter imp = new(dataContainer.Object);
            await SheetContainer.Bake(imp);
            var result
                = await UniTask.RunOnThreadPool(LoadAsync, cancellationToken: ReserveToken);

            Parent
                .Register<IGameConfigProvider>(result.Item1)
                .Register<IActorDataProvider>(result.Item2)
                .Register<ICustomMethodProvider>(result.Item3)
                .Register<IStageDataProvider>(result.Item4)
                .Register<IResearchDataProvider>(result.Item5)
                ;
        }
        protected override UniTask OnReserve()
        {
            Parent
                .Unregister<IGameConfigProvider>()
                .Unregister<IActorDataProvider>()
                .Unregister<ICustomMethodProvider>()
                .Unregister<IStageDataProvider>()
                .Unregister<IResearchDataProvider>()
                ;

            SheetContainer.Dispose();

            return base.OnReserve();
        }

        private async
            UniTask<(GameConfigSession, ActorDataSession, CustomMethodSession, StageDataSession, ResearchDataSession)>
            LoadAsync()
        {
            StatProvider.GetOrCreate(SheetContainer.StatTable);

            var gameConfigSessionTask = CreateSessionOnBackground<GameConfigSession>(
                new GameConfigSession.SessionData(SheetContainer.GameConfigTable));
            var actorDataSessionTask = CreateSessionOnBackground<ActorDataSession>(
                new ActorDataSession.SessionData(SheetContainer.Actors));
            var customMethodSessionTask = CreateSessionOnBackground<CustomMethodSession>(
                new CustomMethodSession.SessionData(SheetContainer.CustomMethodTable));
            var stageDataSessionTask = CreateSessionOnBackground<StageDataSession>(
                new StageDataSession.SessionData(SheetContainer.Stages));
            var researchDataSessionTask = CreateSessionOnBackground<ResearchDataSession>(
                new ResearchDataSession.SessionData(SheetContainer.ResearchTable));

            return await UniTask.WhenAll(
                gameConfigSessionTask,
                actorDataSessionTask,
                customMethodSessionTask,
                stageDataSessionTask,
                researchDataSessionTask);
        }
    }
}