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
using UnityEditor;
using UnityEngine.AddressableAssets;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Provider;

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

            const string DATA_KEY = "Data/_Container.asset";

            var addressableSession = await CreateSession<AddressableSession>(
                new AddressableSession.SessionData("GameData"));
            await addressableSession.Reserve();

            // var list = await Addressables.CheckForCatalogUpdates(true).Task;
            // $"update: {string.Join(", ", list)}".ToLog();
            // if (list.c)
            // // await Addressables.UpdateCatalogs(list).ToUniTask();
            //
            // // await Addressables
            // //     .LoadContentCatalogAsync("https://storage.googleapis.com/vvr-cdn-0/ServerData/iOS/catalog_0.1.json")
            // //     .Task;
            //
            //
            // long downloadSize = await Addressables.GetDownloadSizeAsync("GameData").Task;
            //
            // var  downloadHandle = Addressables.DownloadDependenciesAsync("GameData", true);
            // await downloadHandle.Task;


            // await Addressables.LoadContentCatalogAsync("Game Data")

            // var assetSession = await CreateSession<AssetSession>(
            //     new AssetSession.SessionData(
            //         // "GameData"
            //     ));
            // Register<IAssetProvider>(assetSession);

            SheetContainer = new GameDataSheets(UnityLogger.Default);
            // var dataContainer
            //     = await assetSession.LoadAsync<SheetContainerScriptableObject>(DATA_KEY);
            var dataContainer =
                await Addressables.LoadAssetAsync<SheetContainerScriptableObject>(
                    DATA_KEY).Task;

            ScriptableObjectSheetImporter imp = new(dataContainer);
            await SheetContainer.Bake(imp).AsUniTask();

            StatProvider.GetOrCreate(SheetContainer.StatTable);

            var gameConfigSessionTask = CreateSession<GameConfigSession>(
                new GameConfigSession.SessionData(SheetContainer.GameConfigTable));
            var actorDataSessionTask = CreateSession<ActorDataSession>(
                new ActorDataSession.SessionData(SheetContainer.Actors));
            var customMethodSessionTask = CreateSession<CustomMethodSession>(
                new CustomMethodSession.SessionData(SheetContainer.CustomMethodTable));
            var stageDataSessionTask = CreateSession<StageDataSession>(
                new StageDataSession.SessionData(SheetContainer.Stages));
            var researchDataSessionTask = CreateSession<ResearchDataSession>(
                new ResearchDataSession.SessionData(SheetContainer.ResearchTable));

            await UniTask.WhenAll(
                gameConfigSessionTask,
                actorDataSessionTask,
                customMethodSessionTask,
                stageDataSessionTask,
                researchDataSessionTask);

            Parent
                .Register<IGameConfigProvider>(await gameConfigSessionTask)
                .Register<IActorDataProvider>(await actorDataSessionTask)
                .Register<ICustomMethodProvider>(await customMethodSessionTask)
                .Register<IStageDataProvider>(await stageDataSessionTask)
                .Register<IResearchDataProvider>(await researchDataSessionTask)
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
    }
}