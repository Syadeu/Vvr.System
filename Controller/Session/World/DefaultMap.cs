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
// File created : 2024, 05, 10 20:05

#endregion

using Cysharp.Threading.Tasks;
using Vvr.Controller.Provider;
using Vvr.Provider;

namespace Vvr.Controller.Session.World
{
    [ParentSession(typeof(DefaultWorld))]
    public class DefaultMap : ParentSession<DefaultMap.Data>
        // , IConnector<IActorProvider>
    {
        public struct Data : ISessionData
        {
            public object Id    { get; }
            public int    Index { get; }
        }

        public override string DisplayName => nameof(DefaultMap);

        public DefaultRegion DefaultRegion { get; private set; }

        protected override async UniTask OnInitialize(IParentSession session, Data data)
        {
            // TODO: skip region load
            DefaultRegion = await CreateSession<DefaultRegion>(default);
        }

        // public void Connect(IActorProvider t)
        // {
        //
        // }
        // public void Disconnect()
        // {
        // }
    }
}