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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;

namespace Vvr.Provider
{
    public delegate TProvider ProviderFactoryDelegate<out TProvider>();

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
        [PublicAPI]
        public struct Registry
        {
            abstract class Template
            {
                public abstract IProvider Create();
            }
            sealed class Template<TProvider> : Template where TProvider : IProvider
            {
                private readonly ProviderFactoryDelegate<TProvider> m_FactoryDelegate;

                public Template(ProviderFactoryDelegate<TProvider> t)
                {
                    m_FactoryDelegate = t;
                }

                public override IProvider Create() => m_FactoryDelegate();
            }

            public static Registry Static => default;

            private static readonly ConcurrentDictionary<Type, Template> m_Map = new();

            [ThreadSafe]
            public Registry Lazy<TProvider>(ProviderFactoryDelegate<TProvider> factory = null)
                where TProvider : IProvider
            {
                Type t = typeof(TProvider);
                m_Map[t] = new Template<TProvider>(
                    factory ?? (() => (TProvider)Activator.CreateInstance(t))
                    );

                return this;
            }

            [ThreadSafe]
            public static TProvider Resolve<TProvider>() where TProvider : IProvider
            {
                Type t = typeof(TProvider);

                using (var l = new SemaphoreSlimLock(s_ProviderSemaphore))
                {
                    l.Wait(TimeSpan.FromSeconds(1));

                    if (s_Providers.TryGetValue(t, out var existingProvider))
                    {
                        return (TProvider)existingProvider;
                    }
                }

                if (m_Map.TryGetValue(t, out var template))
                {
                    var provider = template.Create();
                    Register(t, provider);
                    return (TProvider)provider;
                }

                throw new InvalidOperationException($"Provider of type {t.FullName} is not registered");
            }
        }

        public static Provider Static => default;

        private static readonly Dictionary<Type, IProvider>      s_Providers = new();
        private static readonly Dictionary<Type, List<Observer>> s_Observers = new();

        private static readonly SemaphoreSlim s_ProviderSemaphore = new(1, 1);

        /// <summary>
        /// Extracts the base interface of a given type that inherits from <see cref="IProvider"/>.
        /// </summary>
        /// <param name="t">The type to extract the base interface from.</param>
        /// <returns>The base interface of the given type.</returns>
        [Pure]
        public static Type ExtractType(Type t)
        {
            EvaluateExtractType(t);

            const string debugName  = "Provider.ExtractType";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            if (t.IsInterface)
            {
                return t;
            }

            return t.GetInterfaces()
                .First(x =>
                    x != VvrTypeHelper.TypeOf<IProvider>.Type &&
                    x.GetCustomAttribute<AbstractProviderAttribute>() is null &&
                    VvrTypeHelper.InheritsFrom(x, VvrTypeHelper.TypeOf<IProvider>.Type));
        }

        [Conditional("UNITY_EDITOR")]
        private static void EvaluateExtractType(Type t)
        {
            if (t == VvrTypeHelper.TypeOf<IProvider>.Type)
                throw new InvalidOperationException("Raw provider cannot be extract");

            if (!VvrTypeHelper.InheritsFrom<IProvider>(t))
                throw new InvalidOperationException(t.FullName);

            if (t.IsInterface)
            {
                if (t.GetCustomAttribute<AbstractProviderAttribute>() is not null)
                    throw new InvalidOperationException("Abstract provider cannot be extract");
            }
        }

        /// <summary>
        /// Register given provider and resolve all other related <see cref="IConnector{T}"/>s.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to register.</typeparam>
        /// <param name="p">The instance of the provider to register.</param>
        /// <returns>The current instance of the <see cref="Provider"/> struct.</returns>
        [ThreadSafe]
        public Provider Register<TProvider>(TProvider p) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);

            EvaluateType(t);

            Register(t, p);

            return this;
        }

        [ThreadSafe]
        private static void Register(Type t, IProvider p)
        {
            using (var l = new SemaphoreSlimLock(s_ProviderSemaphore))
            {
                l.Wait(TimeSpan.FromSeconds(1));
                if (!s_Providers.TryAdd(t, p))
                    throw new InvalidOperationException("Multiple provider is not allowed");

                if (s_Observers.TryGetValue(t, out var list))
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        item.connect.Invoke(p);
                    }
                }
            }

            $"[Provider] {VvrTypeHelper.ToString(t)} registered".ToLog();
        }

        /// <summary>
        /// Unregister given provider and disconnect all related <see cref="IConnector{T}"/>.
        /// </summary>
        /// <param name="p">The provider to unregister.</param>
        /// <typeparam name="TProvider">Type of the provider.</typeparam>
        /// <returns>The updated <see cref="Provider"/> instance.</returns>
        [ThreadSafe]
        public Provider Unregister<TProvider>(TProvider p) where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);

            EvaluateType(t);

            using (var l = new SemaphoreSlimLock(s_ProviderSemaphore))
            {
                l.Wait(TimeSpan.FromSeconds(1));
                if (s_Providers.Remove(t) &&
                    s_Observers.TryGetValue(t, out var list))
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        item.disconnect.Invoke(p);
                    }
                }
            }

            $"[Provider] {VvrTypeHelper.ToString(t)} unregistered".ToLog();
            return this;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
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

        [ThreadSafe]
        public TProvider Get<TProvider>() where TProvider : IProvider
        {
            Type t = typeof(TProvider);
            t = ExtractType(t);

            EvaluateType(t);

            using var l = new SemaphoreSlimLock(s_ProviderSemaphore);
            l.Wait(TimeSpan.FromSeconds(1));

            s_Providers.TryGetValue(t, out var p);
            return (TProvider)p;
        }

        /// <summary>
        /// Async operation that waits until target provider has resolved.
        /// </summary>
        /// <typeparam name="T">The type of the provider.</typeparam>
        /// <returns>A <see cref="UniTask{T}"/> representing the asynchronous operation.</returns>
        [ThreadSafe]
        public async UniTask<T> GetAsync<T>() where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

            while (true)
            {
                using var l = new SemaphoreSlimLock(s_ProviderSemaphore);
                if (!await l.WaitAsync(TimeSpan.FromSeconds(1)))
                    throw new TimeoutException();

                if (!s_Providers.TryGetValue(t, out var p))
                {
                    continue;
                }

                return (T)p;
            }
            // while (!s_Providers.ContainsKey(t))
            // {
            //     await UniTask.Yield();
            // }
            //
            // using var l = new SemaphoreSlimLock(s_ProviderSemaphore);
            // l.Wait(TimeSpan.FromSeconds(1));
            //
            // return (T)s_Providers[t];
        }

        /// <summary>
        /// Async lazy operation that returns a container that can be resolved at any time.
        /// </summary>
        /// <typeparam name="T">The type of the container.</typeparam>
        /// <returns>The async lazy container of type <typeparamref name="T"/>.</returns>
        [ThreadSafe]
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

            T p;
            using (var l = new SemaphoreSlimLock(s_ProviderSemaphore))
            {
                if (!await l.WaitAsync(TimeSpan.FromSeconds(1)))
                    throw new TimeoutException();

                p = (T)s_Providers[t];
            }
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
        [ThreadSafe]
        public Provider Connect<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

            using (var l = new SemaphoreSlimLock(s_ProviderSemaphore))
            {
                l.Wait(TimeSpan.FromSeconds(1));
                if (s_Providers.TryGetValue(t, out IProvider p))
                {
                    c.Connect((T)p);
                }

                if (!s_Observers.TryGetValue(t, out var list))
                {
                    list           = new();
                    s_Observers[t] = list;
                }

                uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));
                Assert.IsFalse(list.Contains(new Observer() { hash = hash }));

                list.Add(new Observer
                {
                    hash       = hash,
                    disconnect = x => c.Disconnect((T)x),
                    connect    = x => c.Connect((T)x)
                });
            }

            return this;
        }

        /// <summary>
        /// Disconnect and remove from observer.
        /// </summary>
        /// <param name="c">The connector to disconnect.</param>
        /// <typeparam name="T">The type of IProvider.</typeparam>
        /// <returns>The updated Provider.</returns>
        [ThreadSafe]
        public Provider Disconnect<T>(IConnector<T> c) where T : IProvider
        {
            Type t = typeof(T);
            t = ExtractType(t);

            EvaluateType(t);

            using (var l = new SemaphoreSlimLock(s_ProviderSemaphore))
            {
                l.Wait(TimeSpan.FromSeconds(1));

                if (!s_Observers.TryGetValue(t, out var list)) return this;

                if (s_Providers.TryGetValue(t, out var p))
                    c.Disconnect((T)p);

                uint hash = unchecked((uint)c.GetHashCode() ^ FNV1a32.Calculate(t.AssemblyQualifiedName));
                list.Remove(new Observer() { hash = hash });
            }

            return this;
        }
    }
}