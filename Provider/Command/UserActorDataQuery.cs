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
// File created : 2024, 06, 15 20:06

#endregion

using System;
using JetBrains.Annotations;
using Unity.Collections;
using Vvr.Model;

namespace Vvr.Provider.Command
{
    public struct UserActorDataQuery : IDisposable
    {
        public enum CommandType : short
        {
            None = 0,

            SetUserTeamActor,
        }
        public struct SetTeamActorData
        {
            public int index;
            public int id;
        }

        private NativeStream m_Stream;
        private int          m_Count;

        public UserActorDataQuery(NativeStream st)
        {
            m_Stream = st;
            m_Count  = 0;
        }

        public void SetTeamActor(int index, [NotNull] IResolvedActorData actor)
        {
            if (index < 0)
                throw new InvalidOperationException("index cannot be lower than zero");
            if (actor is null)
                throw new InvalidOperationException("Actor cannot be null");

            var wr = m_Stream.AsWriter();
            wr.BeginForEachIndex(m_Count);

            wr.Write((short)CommandType.SetUserTeamActor);
            wr.Write(new SetTeamActorData()
            {
                index = index,
                id    = actor.UniqueId,
            });
            wr.EndForEachIndex();

            m_Count += 2;
        }

        public void Dispose()
        {
            m_Stream = default;
        }
    }
}