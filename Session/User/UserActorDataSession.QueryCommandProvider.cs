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
// File created : 2024, 06, 15 23:06
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Provider.Command;

namespace Vvr.Session.User
{
    public partial class UserActorDataSession
    {
        private readonly SemaphoreSlim m_QueryExecutionSemaphore = new(1, 1);
        private readonly Queue<IQueryCommand<UserActorDataQuery>>
            m_QueryCommands = new();

        private int m_QueryQueued;

        private UniTaskCompletionSource m_QueryFlushSource;

        public UniTask WaitForQueryFlush => m_QueryFlushSource?.Task ?? UniTask.CompletedTask;

        void IQueryCommandProvider<UserActorDataQuery>.Enqueue<TCommand>(TCommand command)
        {
            using (var l = new SemaphoreSlimLock(m_QueryExecutionSemaphore))
            {
                l.Wait(ReserveToken);
                m_QueryCommands.Enqueue(command);
            }

            if (Interlocked.Exchange(ref m_QueryQueued, 1) == 1) return;

            m_QueryFlushSource = new UniTaskCompletionSource();
            UniTask.Void(FlushQueryCommands, ReserveToken);
        }

        private async UniTaskVoid FlushQueryCommands(CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (cancellationToken.IsCancellationRequested)
                return;

            using var l = new SemaphoreSlimLock(m_QueryExecutionSemaphore);
            await l.WaitAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            using NativeStream st = new NativeStream(8, AllocatorManager.Temp);

            var query = new UserActorDataQuery(st);
            {
                while (m_QueryCommands.TryDequeue(out var command))
                {
                    command.Execute(ref query);
                }
            }
            query.Dispose();

            var rdr = st.AsReader();
            ProcessCommandQuery(ref rdr);

            Interlocked.Exchange(ref m_QueryQueued, 0);
            m_QueryFlushSource.TrySetResult();
            m_QueryFlushSource = null;
        }

        private partial void ProcessCommandQuery(ref NativeStream.Reader rdr)
        {
            int count = rdr.BeginForEachIndex(0);

            for (int i = 0; i < count; i++)
            {
                UserActorDataQuery.CommandType t = (UserActorDataQuery.CommandType)rdr.Read<short>();

                switch (t)
                {
                    case UserActorDataQuery.CommandType.SetTeamActor:
                        ProcessCommandData(rdr.Read<UserActorDataQuery.SetTeamActorData>());
                        i++;
                        break;
                    case UserActorDataQuery.CommandType.Flush:
                        Flush();
                        break;
                    case UserActorDataQuery.CommandType.ResetData:
                        LoadCurrentTeam();
                        break;
                    case UserActorDataQuery.CommandType.AddActor:
                        ProcessAddActorData(rdr.Read<UserActorDataQuery.AddActorData>());
                        i++;
                        break;
                    case UserActorDataQuery.CommandType.AddActorRange:
                        ProcessAddActorRange(ref i, ref rdr);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid command type: " + t.ToString());
                }
            }

            rdr.EndForEachIndex();
        }

        private void ProcessAddActorRange(ref int i, ref NativeStream.Reader rdr)
        {
            var meta = rdr.Read<UserActorDataQuery.AddActorRangeData>();
            i++;
            for (int j = 0; j < meta.count; j++, i++)
            {
                int index = rdr.Read<int>();
                Assert.IsFalse(index < 0, "index < 0");
                Assert.IsTrue(index < m_ActorDataProvider.Count, "index < m_ActorDataProvider.Count");

                var actorData = m_ActorDataProvider[index];
                AddActor(actorData);
            }
        }
        private void ProcessAddActorData(UserActorDataQuery.AddActorData data)
        {
            var actorData = m_ActorDataProvider[data.index];

            AddActor(actorData);
        }

        private partial void ProcessCommandData(UserActorDataQuery.SetTeamActorData data)
        {
            if (data.index is < 0 or >= UserDataPath.Actor.TeamCount)
                throw new InvalidOperationException($"{data.index}");

            ResolvedActorData targetData
                = m_ResolvedData.BinarySearch(ResolvedActorData.KeySelector, data.id);
            Assert.IsNotNull(targetData);

            // If target actor is in team
            if (TryGetCurrentTeamIndex(targetData, out int targetTeamIndex))
            {
                // If trying to insert at same index
                if (targetTeamIndex == data.index)
                    // XXX: need to consideration that this operation might unintentional.
                    return;

                // Can be swap if both actors in the same team
                m_CurrentTeam[targetTeamIndex] = m_CurrentTeam[data.index];
            }

            m_CurrentTeam[data.index] = targetData;
        }
    }
}