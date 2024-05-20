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
// File created : 2024, 05, 17 15:05

#endregion

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a session for stage data.
    /// </summary>
    /// <seealso cref="Vvr.Provider.IStageDataProvider" />
    [UsedImplicitly]
    [ParentSession(typeof(GameDataSession))]
    public class StageDataSession : ChildSession<StageDataSession.SessionData>,
        IStageDataProvider
    {
        public struct SessionData : ISessionData
        {
            public readonly StageSheet sheet;

            public SessionData(StageSheet s)
            {
                sheet = s;
            }
        }

        public override string DisplayName => nameof(StageDataSession);

        public IStageData this[string key] => Data.sheet[key];
        public IEnumerable<string>     Keys   => Data.sheet.Keys;
        public IEnumerable<IStageData> Values => Data.sheet;

        public int Count => Data.sheet.Count;

        public bool ContainsKey(string key) => Data.sheet.Contains(key);
        public bool TryGetValue(string    key, out IStageData value)
        {
            value = null;
            if (!Data.sheet.TryGetValue(key, out var v)) return false;

            value = v;
            return true;
        }

        public IStageData ElementAt(int index)
        {
            Assert.IsFalse(index < 0);
            if (Data.sheet.Count <= index) return null;

            return Data.sheet[index];
        }

        public IEnumerator<KeyValuePair<string, IStageData>> GetEnumerator()
        {
            foreach (var x in Data.sheet)
            {
                yield return new KeyValuePair<string, IStageData>(x.Id, x);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}