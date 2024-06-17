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
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Vvr.Model;

namespace Vvr.Provider.Command
{
    /// <summary>
    /// Represents a query to retrieve user actor data.
    /// </summary>
    [PublicAPI]
    public struct UserActorDataQuery : IDisposable
    {
        public enum CommandType : short
        {
            None = 0,

            Flush,
            ResetData,
            SetTeamActor,

            AddActor,
            AddActorRange,
        }
        public struct SetTeamActorData
        {
            public int index;
            public int id;
        }
        public struct AddActorData
        {
            public int index;
        }
        public struct AddActorRangeData
        {
            public short count;
        }

        [PublicAPI]
        public struct AddActorScope : IDisposable
        {
            private          NativeStream         m_Stream;
            private          NativeReference<int> m_Count;
            private readonly List<int>            m_Indices;

            internal AddActorScope(NativeStream st, NativeReference<int> count)
            {
                m_Stream  = st;
                m_Count   = count;
                m_Indices = new List<int>();
            }

            public void Add(IActorData data)
            {
                m_Indices.Add(data.Index);
            }

            public void Dispose()
            {
                if (m_Indices.Count > short.MaxValue)
                    throw new OverflowException();

                var wr = m_Stream.AsWriter();
                wr.BeginForEachIndex(m_Count.Value);
                wr.Write((short)CommandType.AddActorRange);
                wr.Write(new AddActorRangeData()
                {
                    count = (short)m_Indices.Count
                });
                m_Count.Value += 2;

                for (int i = 0; i < m_Indices.Count; i++)
                {
                    wr.Write(m_Indices[i]);
                }

                m_Count.Value += m_Indices.Count;

                wr.EndForEachIndex();
                m_Indices.Clear();
            }
        }

        private NativeStream         m_Stream;
        private NativeReference<int> m_Count;

        public UserActorDataQuery(NativeStream st)
        {
            m_Stream = st;
            m_Count  = new NativeReference<int>(AllocatorManager.Temp);
        }

        public AddActorScope AddActorRange()
        {
            return new AddActorScope(m_Stream, m_Count);
        }
        public void AddActor(IActorData data)
        {
            var wr = m_Stream.AsWriter();
            wr.BeginForEachIndex(m_Count.Value);

            wr.Write((short)CommandType.AddActor);
            wr.Write(new AddActorData()
            {
                index = data.Index,
            });
            wr.EndForEachIndex();

            m_Count.Value += 2;
        }

        /// <summary>
        /// Sets the team actor for a given index.
        /// </summary>
        /// <remarks>
        /// Need to call <seealso cref="Flush"/> to permanent change.
        /// </remarks>
        /// <param name="index">The index in the team.</param>
        /// <param name="actor">The resolved actor data.</param>
        public void SetTeamActor(int index, [NotNull] IResolvedActorData actor)
        {
            if (index < 0)
                throw new InvalidOperationException("index cannot be lower than zero");
            if (actor is null)
                throw new InvalidOperationException("Actor cannot be null");

            var wr = m_Stream.AsWriter();
            wr.BeginForEachIndex(m_Count.Value);

            wr.Write((short)CommandType.SetTeamActor);
            wr.Write(new SetTeamActorData()
            {
                index = index,
                id    = actor.UniqueId,
            });
            wr.EndForEachIndex();

            m_Count.Value += 2;
        }

        public void ResetData()
        {
            var wr = m_Stream.AsWriter();
            wr.BeginForEachIndex(m_Count.Value);

            wr.Write((short)CommandType.ResetData);

            wr.EndForEachIndex();
            m_Count.Value++;
        }

        public void Flush()
        {
            var wr = m_Stream.AsWriter();
            wr.BeginForEachIndex(m_Count.Value);

            wr.Write((short)CommandType.Flush);

            wr.EndForEachIndex();
            m_Count.Value++;
        }

        public void Dispose()
        {
            m_Count.Dispose();
        }
    }
}