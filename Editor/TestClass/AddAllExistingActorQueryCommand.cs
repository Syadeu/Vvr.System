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
// File created : 2024, 06, 17 12:06

#endregion

using JetBrains.Annotations;
using NUnit.Framework;
using Vvr.Provider;
using Vvr.Provider.Command;
using Vvr.Session.Provider;
using Vvr.Session.World;

namespace Vvr.TestClass
{
    [PublicAPI]
    public struct AddAllExistingActorQueryCommand : IQueryCommand<UserActorDataQuery>
    {
        [CanBeNull]
        private string m_PrefixId;

        public AddAllExistingActorQueryCommand([CanBeNull] string prefixId)
        {
            m_PrefixId = prefixId;
        }

        void IQueryCommand<UserActorDataQuery>.Execute(ref UserActorDataQuery query)
        {
            IUserActorProvider provider = GameWorld.World.GetProviderRecursive<IUserActorProvider>();
            Assert.NotNull(provider);

            IActorDataProvider dataProvider = GameWorld.World.GetProviderRecursive<IActorDataProvider>();
            Assert.NotNull(dataProvider);

            using var addScope = query.AddActorRange();
            for (int i = 0; i < dataProvider.Count; i++)
            {
                var e = dataProvider[i];
                if (m_PrefixId is null || m_PrefixId.IsNullOrEmpty())
                    addScope.Add(e);
                else
                {
                    if (e.Id.StartsWith(m_PrefixId))
                        addScope.Add(e);
                }
            }
        }
    }
}