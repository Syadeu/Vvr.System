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
// File created : 2024, 05, 10 16:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vvr.Controller.Actor;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Provider
{
    /// <summary>
    /// Represents an actor provider.
    /// </summary>
    public interface IActorProvider : IProvider
    {
        /// <summary>
        /// Resolves an actor based on the given data.
        /// </summary>
        /// <remarks>
        /// Due to resolved actor is concrete actor should not have any view targets.
        /// </remarks>
        /// <param name="data">The data used to resolve the actor.</param>
        /// <returns>The resolved actor.</returns>
        IActor Resolve(ActorSheet.Row data);
    }

    internal sealed class ActorProvider : IActorProvider, IDisposable
    {
        struct CachedActor : IComparable<CachedActor>
        {
            public          uint   hash;
            public readonly IActor actor;

            public CachedActor(IActor t)
            {
                hash  = FNV1a32.Calculate(t.DataID);
                actor = t;
            }

            public int CompareTo(CachedActor other)
            {
                return hash.CompareTo(other.hash);
            }
        }

        private AsyncLazy<IActorDataProvider> m_DataProvider;

        private readonly List<CachedActor> m_ResolvedActors = new();

        public ActorProvider()
        {
            m_DataProvider = Vvr.Provider.Provider.Static.GetLazyAsync<IActorDataProvider>();
        }

        public void Dispose()
        {
            for (int i = 0; i < m_ResolvedActors.Count; i++)
            {
                IActor e = m_ResolvedActors[i].actor;

                e.Release();
            }

            m_DataProvider = null;
        }

        public IActor Resolve(ActorSheet.Row data)
        {
            int i = m_ResolvedActors.BinarySearch(new CachedActor() { hash = FNV1a32.Calculate(data.Id) });
            if (0 <= i) return m_ResolvedActors[i].actor;

            Actor.Actor actor = ScriptableObject.CreateInstance<Actor.Actor>();

            m_ResolvedActors.Add(new CachedActor(actor));
            m_ResolvedActors.Sort();
            return actor;
        }
    }
}