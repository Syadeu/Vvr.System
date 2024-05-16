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
// File created : 2024, 05, 14 01:05
#endregion

using System.Collections.Generic;
using Cathei.BakingSheet.Unity;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Asset
{
    public class AssetController : IAsset
    {
        private readonly IReadOnlyDictionary<AssetType, AddressablePath> m_AssetsPath;

        public object this[AssetType t] => m_AssetsPath[t].FullPath;

        public IAssetProvider AssetProvider { get; private set; }

        public AssetController(IReadOnlyDictionary<AssetType, AddressablePath> t)
        {
            m_AssetsPath = t;
        }

        void IConnector<IAssetProvider>.Connect(IAssetProvider    t) => AssetProvider = t;
        void IConnector<IAssetProvider>.Disconnect(IAssetProvider t) => AssetProvider = null;
    }
}