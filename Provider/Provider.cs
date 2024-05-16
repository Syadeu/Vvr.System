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
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace Vvr.Provider
{
    /// <summary>
    /// Global provider that provides all <see cref="IProvider"/> to requesters.
    /// </summary>
    public struct Provider
    {
        struct Observer : IEquatable<Observer>
        {
            public uint              hash;
            public Action<IProvider> connect;
            public Action<IProvider> disconnect;

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
                    return (hash.GetHashCode() * 397)
                           ^ (connect    != null ? connect.GetHashCode() : 0)
                           ^ (disconnect != null ? disconnect.GetHashCode() : 0)
                           ;
                }
            }
        }
        public static Provider Static { get; } = default;

        private static readonly Dictionary<Type, IProvider>      s_Providers = new();
        private static readonly Dictionary<Type, List<Observer>> s_Observers = new();

        /// <summary>
        /// Extracts the base interface of a given type that inherits from <see cref="IProvider"/>.
        /// </summary>
        /// <param name="t">The type to extract the base interface from.</param>
        /// <returns>The base interface of the given type.</returns>
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
        /// Register given provider and resolve all other related <see cref="IConnector{T}"/>s.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to register.</typeparam>
        /// <param name="p">The instance of the provider to register.</param>
        /// <returns>The current instance of the <see cref="Provider"/> struct.</returns>
        public Provider Register<TProvider>(TProvider p) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);

            EvaluateType(t);

            if (!s_Providers.TryAdd(t, p))
                throw new InvalidOperationException("Multiple provider is not allowed");

            if (s_Observers.TryGetValue(t, out var list))
            {
                foreach (var item in list)
                {
                    item.connect.Invoke(p);
                }
            }

            $"[Provider] {VvrTypeHelper.ToString(t)} registered".ToLog();

            return this;
        }

        /// <summary>
        /// Unregister given provider and disconnect all related <see cref="IConnector{T}"/>.
        /// </summary>
        /// <param name="p">The provider to unregister.</param>
        /// <typeparam name="TProvider">Type of the provider.</typeparam>
        /// <returns>The updated <see cref="Provider"/> instance.</returns>
        public Provider Unregister<TProvider>(TProvider p) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);

            EvaluateType(t);

            if (s_Providers.Remove(t))
            {
                if (s_Observers.TryGetValue(t, out var list))
                {
                    foreach (var item in list)
                    {
                        item.disconnect.Invoke(p);
                    }
                }
            }

            $"[Provider] {VvrTypeHelper.ToString(t)} unregistered".ToLog();
            return this;
        }

        private static void EvaluateType(Type providerType)
        {
            Assert.IsNotNull(providerType);

            if (VvrTypeHelper.TypeOf<IProvider>.Type == providerType)
                throw new InvalidOperationException("Input was base provider type");

            var localAtt = providerType.GetCustomAttribute<LocalProviderAttribute>();
            if (localAtt != null)
                throw new InvalidOperationException(
                    $"Local provider should not interact with global provider. {providerType.FullName}");
        }

        /// <summary>
        /// Async operation that waits until target provider has resolved.
        /// </summary>
        /// <typeparam name="T">The type of the provider.</typeparam>
        /// <returns>A <see cref="UniTask{T}"/> representing the asynchronous operation.</returns>
        public async UniTask<T> GetAsync<T>() where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

            while (!s_Providers.ContainsKey(t))
            {
                await UniTask.Yield();
            }

            return (T)s_Providers[t];
        }

        /// <summary>
        /// Async lazy operation that returns a container that can be resolved at any time.
        /// </summary>
        /// <typeparam name="T">The type of the container.</typeparam>
        /// <returns>The async lazy container of type <typeparamref name="T"/>.</returns>
        public AsyncLazy<T> GetLazyAsync<T>() where T : IProvider
        {
            Func<UniTask<T>> p = GetAsync<T>;
            return UniTask.Lazy(p);
        }

        /// <summary>
        /// Asynchronous operation that waits until the target provider has resolved and connects the given connector to the provider.
        /// </summary>
        /// <typeparam name="T">The type of the provider that the connector connects to. Must implement <see cref="IProvider"/>.</typeparam>
        /// <param name="c">The connector instance that needs to be connected to the provider.</param>
        /// <returns>A <see cref="UniTask{T}"/> representing the asynchronous operation. The task will complete with the resolved provider instance.</returns>
        public async UniTask<T> ConnectAsync<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

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
                disconnect = x => c.Disconnect((T)x),
                connect = x => c.Connect((T)x)
            });

            return p;
        }

        /// <summary>
        /// Connect to request provider and subscribe.
        /// </summary>
        /// <remarks>
        /// If requested provider already resolved, returns immediately.
        /// </remarks>
        /// <param name="c">The connector to be connected.</param>
        /// <typeparam name="T">The type of the provider to connect to.</typeparam>
        /// <returns>The current instance of the <see cref="Provider"/> struct.</returns>
        public Provider Connect<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

            if (s_Providers.TryGetValue(t, out var p))
            {
                c.Connect((T)p);
            }

            if (!s_Observers.TryGetValue(t, out var list))
            {
                list           = new();
                s_Observers[t] = list;
            }

            uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));
            Assert.IsFalse(list.Contains(new Observer(){hash = hash}));

            list.Add(new Observer
            {
                hash       = hash,
                disconnect = x => c.Disconnect((T)x),
                connect    = x => c.Connect((T)x)
            });

            return this;
        }

        /// <summary>
        /// Disconnect and remove from observer.
        /// </summary>
        /// <param name="c">The connector to disconnect.</param>
        /// <typeparam name="T">The type of IProvider.</typeparam>
        /// <returns>The updated Provider.</returns>
        public Provider Disconnect<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

            if (!s_Observers.TryGetValue(t, out var list)) return this;

            if (s_Providers.TryGetValue(t, out var p))
                c.Disconnect((T)p);

            uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));
            list.Remove(new Observer() { hash = hash });

            return this;
        }
    }
}