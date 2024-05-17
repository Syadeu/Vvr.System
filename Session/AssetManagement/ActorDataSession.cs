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
// File created : 2024, 05, 17 14:05

#endregion

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a session that provides actor data.
    /// </summary>
    [UsedImplicitly]
    [ParentSession(typeof(GameDataSession))]
    public class ActorDataSession : ChildSession<ActorDataSession.SessionData>,
        IActorDataProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly ActorSheet sheet;

            public SessionData(ActorSheet s)
            {
                sheet = s;
            }
        }

        public override string DisplayName => nameof(ActorDataSession);

        public ActorSheet.Row this[int index] => Data.sheet[index];
        public int Count => Data.sheet.Count;

        public ActorSheet.Row Resolve(string key)
        {
            return Data.sheet[key];
        }

        public IEnumerator<ActorSheet.Row> GetEnumerator() => Data.sheet.GetEnumerator();
        IEnumerator IEnumerable.           GetEnumerator() => ((IEnumerable)Data.sheet).GetEnumerator();
    }
}