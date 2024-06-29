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

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Collections;
using Vvr.Model.Wallet;
using Vvr.Provider;
using Vvr.Provider.Command;

namespace Vvr.Session.User
{
    [UsedImplicitly]
    public sealed class UserWalletDataSession : ChildSession<UserWalletDataSession.SessionData>,
        IUserWalletProvider
    {
        public struct SessionData : ISessionData
        {
            public IDataProvider dataProvider;
        }

        public override string DisplayName => nameof(UserWalletDataSession);

        public float this[WalletType walletType]
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }
        public UniTask WaitForQueryFlush => UniTask.CompletedTask;

        protected override UniTask OnInitialize(IParentSession session, SessionData data)
        {
            return base.OnInitialize(session, data);
        }

        public void Enqueue<TCommand>(TCommand command) where TCommand : IQueryCommand<UserWalletQuery>
        {
            using NativeStream st = new NativeStream(2, AllocatorManager.Temp);

            var query = new UserWalletQuery(st);
            {
                command.Execute(ref query);
            }
            query.Dispose();

            var rdr = st.AsReader();
            ProcessCommandQuery(ref rdr);
        }

        private void ProcessCommandQuery(ref NativeStream.Reader rdr)
        {
            int count = rdr.BeginForEachIndex(0);
            while (0 < count--)
            {
                UserWalletQuery.Entry e = rdr.Read<UserWalletQuery.Entry>();


            }
            rdr.EndForEachIndex();
        }

        public void Flush()
        {
            throw new System.NotImplementedException();
        }
    }
}