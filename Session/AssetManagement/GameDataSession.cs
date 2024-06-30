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
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.Provider;

namespace Vvr.Session.AssetManagement
{
    /// <summary>
    /// Represents a session used for managing game data.
    /// </summary>
    [UsedImplicitly]
    [ProviderSession(
        typeof(IGameConfigProvider),
        typeof(IActorDataProvider),
        typeof(ICustomMethodProvider),
        typeof(IStageDataProvider),
        typeof(IResearchDataProvider),
        typeof(IStatConditionProvider),
        typeof(IWalletTypeProvider)
        )]
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
                new AssetSession.SessionData()
                {
                    useEditorAsset = true
                });
            Register<IAssetProvider>(assetSession);

            try
            {
                SheetContainer = await UniTask.Create(() => BakeSheetAsync(assetSession));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            var result = await UniTask.WhenAll(
                CreateSession<GameConfigSession>(
                    new GameConfigSession.SessionData(SheetContainer.GameConfigTable)),
                CreateSession<ActorDataSession>(
                    new ActorDataSession.SessionData(SheetContainer.Actors)),
                CreateSession<CustomMethodSession>(
                    new CustomMethodSession.SessionData(SheetContainer.CustomMethodTable)),
                CreateSession<StageDataSession>(
                    new StageDataSession.SessionData(SheetContainer.Stages)),
                CreateSession<ResearchDataSession>(
                    new ResearchDataSession.SessionData(SheetContainer.ResearchTable)),
                CreateSession<StatDataSession>(
                    new StatDataSession.SessionData(SheetContainer.StatTable)),
                CreateSession<WalletDataSession>(
                    new WalletDataSession.SessionData(SheetContainer.WalletTable))
                );

            Parent
                .Register<IGameConfigProvider>(result.Item1)
                .Register<IActorDataProvider>(result.Item2)
                .Register<ICustomMethodProvider>(result.Item3)
                .Register<IStageDataProvider>(result.Item4)
                .Register<IResearchDataProvider>(result.Item5)
                .Register<IStatConditionProvider>(result.Item6)
                .Register<IWalletTypeProvider>(result.Item7)
                ;
        }

        private async UniTask<GameDataSheets> BakeSheetAsync(IAssetProvider assetSession)
        {
            const string DATA_KEY = "Data/_Container.asset";

            try
            {
                var result = new GameDataSheets(UnityLogger.Default);
                IImmutableObject<SheetContainerScriptableObject> dataContainer
                        = await assetSession.LoadAsync<SheetContainerScriptableObject>(DATA_KEY)
                            .AttachExternalCancellation(ReserveToken)
                    ;

                ScriptableObjectSheetImporter imp = new(dataContainer.Object);
                await result.Bake(imp)
                        .AsUniTask()
                        .AttachExternalCancellation(ReserveToken)
                    ;

                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        protected override UniTask OnReserve()
        {
            Parent
                .Unregister<IGameConfigProvider>()
                .Unregister<IActorDataProvider>()
                .Unregister<ICustomMethodProvider>()
                .Unregister<IStageDataProvider>()
                .Unregister<IResearchDataProvider>()
                .Unregister<IStatConditionProvider>()
                .Unregister<IWalletTypeProvider>()
                ;

            SheetContainer.Dispose();

            return base.OnReserve();
        }
    }
}