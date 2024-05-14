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
// File created : 2024, 05, 10 02:05

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace Vvr.MPC.Provider
{
    /// <summary>
    /// Global provider that provides all <see cref="IProvider"/> to requesters.
    /// </summary>
    public struct Provider
    {
        struct Observer : IEquatable<Observer>
        {
            public uint              hash;
            public Action<IProvider> action;

            public bool Equals(Observer other)
            {
                return hash.Equals(other.hash);
            }

            public override bool Equals(object obj)
            {
                return obj is Observer other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (hash.GetHashCode() * 397) ^ (action != null ? action.GetHashCode() : 0);
                }
            }
        }
        public static Provider Static { get; } = default;

        private static readonly Dictionary<Type, IProvider>      s_Providers = new();
        private static readonly Dictionary<Type, List<Observer>> s_Observers = new();

        public static Type ExtractType(Type t)
        {
            if (!VvrTypeHelper.InheritsFrom<IProvider>(t))
                throw new InvalidOperationException();

            if (t.IsInterface) return t;

            return t.GetInterfaces()
                .First(x =>
                    x != VvrTypeHelper.TypeOf<IProvider>.Type &&
                    VvrTypeHelper.InheritsFrom(x, VvrTypeHelper.TypeOf<IProvider>.Type));
        }

        /// <summary>
        /// Register given provider and resolve all other related <see cref="IConnector{T}"/>s
        /// </summary>
        /// <param name="p"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public Provider Register<TProvider>(TProvider p) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);
            if (!s_Providers.TryAdd(t, p))
                throw new InvalidOperationException("Multiple provider is not allowed");

            if (s_Observers.TryGetValue(t, out var list))
            {
                foreach (var item in list)
                {
                    item.action.Invoke(p);
                }
            }

            $"[Provider] {VvrTypeHelper.ToString(t)} registered".ToLog();

            return this;
        }
        /// <summary>
        /// Unregister given provider and disconnect all related <see cref="IConnector{T}"/>
        /// </summary>
        /// <param name="p"></param>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public Provider Unregister<TProvider>(TProvider p) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);
            s_Providers.Remove(t);

            if (s_Observers.TryGetValue(t, out var list))
            {
                foreach (var item in list)
                {
                    item.action.Invoke(null);
                }
            }

            $"[Provider] {VvrTypeHelper.ToString(t)} unregistered".ToLog();
            return this;
        }

        /// <summary>
        /// Async operation that waits until target provider has resolved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<T> GetAsync<T>() where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);
            while (!s_Providers.ContainsKey(t))
            {
                await UniTask.Yield();
            }

            return (T)s_Providers[t];
        }

        /// <summary>
        /// Async lazy operation that returns container can be resolved by any time.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AsyncLazy<T> GetLazyAsync<T>() where T : IProvider
        {
            Func<UniTask<T>> p = GetAsync<T>;
            return UniTask.Lazy(p);
        }
        /// <summary>
        /// Async operation that waits until target provider has resolved.
        /// </summary>
        /// <param name="c"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<T> ConnectAsync<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);
            while (!s_Providers.ContainsKey(t))
            {
                await UniTask.Yield();
            }

            T p = (T)s_Providers[t];
            c.Connect(p);

            if (!s_Observers.TryGetValue(t, out var list))
            {
                list           = new();
                s_Observers[t] = list;
            }

            uint hash = unchecked((uint)c.GetHashCode());
            Assert.IsFalse(list.Contains(new Observer() { hash = hash }));

            list.Add(new Observer
            {
                hash = hash,
                action = x =>
                {
                    if (x == null) c.Disconnect();
                    else c.Connect((T)x);
                }
            });

            return p;
        }

        /// <summary>
        /// Connect to request provider and subscribe.
        /// </summary>
        /// <remarks>
        /// If requested provider already resolved, returns immediately.
        /// </remarks>
        /// <param name="c"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Provider Connect<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);
            if (s_Providers.TryGetValue(t, out var p))
            {
                c.Connect((T)p);
            }

            if (!s_Observers.TryGetValue(t, out var list))
            {
                list           = new();
                s_Observers[t] = list;
            }

            uint hash = unchecked((uint)c.GetHashCode());
            Assert.IsFalse(list.Contains(new Observer(){hash = hash}));

            list.Add(new Observer
            {
                hash   = hash,
                action = x =>
                {
                    if (x == null) c.Disconnect();
                    else c.Connect((T)x);
                }
            });

            return this;
        }
        /// <summary>
        /// Disconnect and remove from observer.
        /// </summary>
        /// <param name="c"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Provider Disconnect<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            c.Disconnect();
            if (s_Observers.TryGetValue(t, out var list))
            {
                uint hash = unchecked((uint)c.GetHashCode());
                list.Remove(new Observer() { hash = hash });
            }

            return this;
        }
    }
}