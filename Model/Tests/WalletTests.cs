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
// File created : 2024, 06, 29 12:06

#endregion

using NUnit.Framework;
using Vvr.Model.Wallet;
using Vvr.TestClass;

namespace Vvr.Model.Tests
{
    [TestFixture]
    public sealed class WalletTests
    {
        private ShortFlag64<WalletType> GetWalletType(short i)
        {
            return (WalletType)i;
        }

        [Test]
        public void CreateTest()
        {
            Wallet.Wallet w = Wallet.Wallet.Create(GetWalletType(0) | GetWalletType(1));

            Assert.IsTrue(w.Types.Contains((WalletType)0));
            Assert.IsTrue(w.Types.Contains((WalletType)1));

            TestUtils.Approximately(0, w[(WalletType)0]);
            TestUtils.Approximately(0, w[(WalletType)1]);
        }
        [Test]
        public void AddTest_0()
        {
            Wallet.Wallet w = Wallet.Wallet.Create(GetWalletType(0) | GetWalletType(1));

            w[0]             = 100;
            w[(WalletType)1] = 500;

            TestUtils.Approximately(100, w[(WalletType)0]);
            TestUtils.Approximately(500, w[(WalletType)1]);
        }
        [Test]
        public void AddTest_1()
        {
            Wallet.Wallet
                w0 = Wallet.Wallet.Create(GetWalletType(0) | GetWalletType(1)),
                w1 = Wallet.Wallet.Create(GetWalletType(0) | GetWalletType(1))
                ;

            w0[0]             = 100;
            w0[(WalletType)1] = 500;
            w1[0]             = 100;
            w1[(WalletType)1] = 500;

            var r = w0 + w1;

            TestUtils.Approximately(200, r[(WalletType)0]);
            TestUtils.Approximately(1000, r[(WalletType)1]);
        }

        [Test]
        public void MinusTest_0()
        {
            Wallet.Wallet
                w0 = Wallet.Wallet.Create(GetWalletType(0) | GetWalletType(1)),
                w1 = Wallet.Wallet.Create(GetWalletType(0) | GetWalletType(1))
                ;

            w0[0]             = 100;
            w0[(WalletType)1] = 500;
            w1[0]             = 100;
            w1[(WalletType)1] = 500;

            var r = w0 - w1;

            TestUtils.Approximately(0, r[(WalletType)0]);
            TestUtils.Approximately(0, r[(WalletType)1]);
        }
    }
}