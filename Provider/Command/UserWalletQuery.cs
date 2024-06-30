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
// File created : 2024, 06, 29 22:06

#endregion

using System;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine.Assertions;
using Vvr.Crypto;
using Vvr.Model.Wallet;

namespace Vvr.Provider.Command
{
    [PublicAPI]
    public struct UserWalletQuery : IDisposable
    {
        public struct Entry
        {
            public short       walletType;
            public CryptoFloat value;

            public Entry(WalletType t, float v)
            {
                walletType = (short)t;
                value      = v;
            }
        }

        private NativeStream         m_Stream;
        private NativeReference<int> m_Count;

        public UserWalletQuery(NativeStream st)
        {
            m_Stream = st;
            m_Count  = new NativeReference<int>(AllocatorManager.Temp);
        }
        public void Dispose()
        {
            m_Count.Dispose();
        }

        public void Increment(WalletType t, float v)
        {
            Assert.IsTrue(0 <= v);
            var wr = m_Stream.AsWriter();

            wr.BeginForEachIndex(m_Count.Value);
            wr.Write(new Entry(t, v));
            wr.EndForEachIndex();

            m_Count.Value++;
        }
        public void Decrement(WalletType t, float v)
        {
            Assert.IsTrue(0 <= v);
            var wr = m_Stream.AsWriter();

            wr.BeginForEachIndex(m_Count.Value);
            wr.Write(new Entry(t, -v));
            wr.EndForEachIndex();

            m_Count.Value++;
        }
    }
}