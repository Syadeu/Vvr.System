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
// File created : 2024, 06, 29 20:06

#endregion

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Model.Wallet;
using Vvr.Provider;

namespace Vvr.Session.AssetManagement
{
    [UsedImplicitly]
    public sealed class WalletDataSession : ChildSession<WalletDataSession.SessionData>,
        IWalletTypeProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly WalletSheet sheet;

            public SessionData(WalletSheet s)
            {
                sheet = s;
            }
        }

        public override string DisplayName => nameof(DisplayName);

        public IWalletType this[WalletType type] => Data.sheet[(short)type];

        public IEnumerator<KeyValuePair<WalletType, IWalletType>> GetEnumerator()
        {
            for (int i = 0; i < Data.sheet.Count; i++)
            {
                var walletType = (WalletType)i;
                yield return new KeyValuePair<WalletType, IWalletType>(walletType, Data.sheet[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}