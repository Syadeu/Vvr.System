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
// File created : 2024, 05, 14 14:05

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session.Provider
{
    internal sealed class ActorDataProvider : IActorDataProvider, IDisposable
    {
        private readonly ActorSheet m_Sheet;

        public ActorDataProvider(ActorSheet sheet)
        {
            m_Sheet = sheet;
        }
        public ActorSheet.Row Resolve(string key)
        {
            return m_Sheet[key];
        }

        public void Dispose()
        {
        }

        public IEnumerator<ActorSheet.Row> GetEnumerator() => m_Sheet.GetEnumerator();
        IEnumerator IEnumerable.           GetEnumerator() => ((IEnumerable)m_Sheet).GetEnumerator();

        public int Count => m_Sheet.Count;

        public ActorSheet.Row this[int index] => m_Sheet[index];
    }
}