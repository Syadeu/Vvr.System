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
// File created : 2024, 05, 14 20:05

#endregion

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Session
{
    /// <summary>
    /// Represents a container for registering and resolving dependencies.
    /// </summary>
    [PublicAPI]
    public interface IDependencyContainer
    {
        /// <summary>
        /// Connects the provider interface to the session.
        /// </summary>
        /// <param name="pType">The type of the provider.</param>
        /// <param name="provider">The instance of the provider.</param>
        /// <remarks>
        /// This method connects the specified provider interface to the session for internal use within the ChildSession class.
        /// It is used to establish communication between the session and the provider.
        /// </remarks>
        [NotNull, ThreadSafe]
        IDependencyContainer Register([NotNull] Type pType, [NotNull] IProvider provider);

        /// <summary>
        /// Connects the specified provider interface to the session for internal use within the ChildSession class.
        /// It is used to establish communication between the session and the provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <param name="provider">The instance of the provider.</param>
        /// <returns>The updated instance of the IDependencyContainer.</returns>
        /// <remarks>
        /// This method connects the provider interface to the session.
        /// It is used to establish communication between the session and the provider.
        /// </remarks>
        [NotNull, ThreadSafe]
        IDependencyContainer Register<TProvider>([NotNull] TProvider provider) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the provider interface from the session.
        /// </summary>
        /// <param name="pType">The type of the provider interface.</param>
        /// <remarks>
        /// This method disconnects the specified provider interface from the session. It is used to terminate communication between the session and the provider.
        /// </remarks>
        [NotNull, ThreadSafe]
        IDependencyContainer Unregister([NotNull] Type pType);

        /// <summary>
        /// Unregisters the specified provider from the dependency container.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to unregister.</typeparam>
        /// <returns>The dependency container instance after unregistering the provider.</returns>
        [NotNull, ThreadSafe]
        IDependencyContainer Unregister<TProvider>() where TProvider : IProvider;

        /// <summary>
        /// Connects a connector to the session for the specified provider.
        /// </summary>
        /// <remarks>
        /// If target provider already registerd, connect immediately.
        /// </remarks>
        /// <typeparam name="TProvider">The type of the provider.</typeparam>
        /// <param name="c">The connector to connect.</param>
        /// <returns>void</returns>
        [NotNull, ThreadSafe]
        [ContractAnnotation("c:null => halt")]
        IDependencyContainer Connect<TProvider>([NotNull] IConnector<TProvider> c) where TProvider : IProvider;

        /// <summary>
        /// Disconnects the specified provider from the game session.
        /// </summary>
        /// <typeparam name="TProvider">The type of provider to disconnect.</typeparam>
        /// <param name="c">The connector of the provider.</param>
        [NotNull, ThreadSafe]
        [ContractAnnotation("c:null => halt")]
        IDependencyContainer Disconnect<TProvider>([NotNull] IConnector<TProvider> c) where TProvider : IProvider;

        /// <summary>
        /// Unregisters all connected providers from the child session.
        /// </summary>
        [ThreadSafe]
        void UnregisterAll();

        /// <summary>
        /// Disconnects all observers from the child session.
        /// This method removes all observers that are currently connected to the child session.
        /// </summary>
        void DisconnectAllObservers();

        bool TryGetProvider([NotNull] Type providerType, out IProvider provider);

        /// <summary>
        /// Recursively gets the provider of the specified type.
        /// </summary>
        /// <typeparam name="TProvider">The type of the provider to retrieve.</typeparam>
        /// <returns>The provider of the specified type if it exists, otherwise null.</returns>
        [CanBeNull]
        TProvider GetProviderRecursive<TProvider>() where TProvider : class, IProvider;

        /// <summary>
        /// Retrieves a provider recursively from the game session hierarchy based on the specified provider type.
        /// </summary>
        /// <remarks>
        /// This method searches for a provider of the specified type in the current session and its parent sessions recursively.
        /// If a match is found, the provider is returned. Otherwise, null is returned.
        /// </remarks>
        /// <param name="providerType">The type of the provider to retrieve.</param>
        /// <returns>The provider of the specified type, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="providerType"/> is null.</exception>
        [CanBeNull]
        [ContractAnnotation("providerType:null => halt")]
        IProvider GetProviderRecursive([NotNull] Type providerType);

        /// <summary>
        /// Retrieves an enumerable collection of key-value pairs that represent the registered providers in the dependency container.
        /// </summary>
        /// <returns>
        /// An enumerable collection of key-value pairs, where the key is the type of the provider and the value is the instance of the provider.
        /// </returns>
        [NotNull]
        IEnumerable<KeyValuePair<Type, IProvider>> GetEnumerable();
    }

    public record DependencyInfo(Type Type, IReadOnlyList<Type> ConnectorTypes)
    {
        public Type                Type           { get; } = Type;
        public IReadOnlyList<Type> ConnectorTypes { get; } = ConnectorTypes;
    }

    /// <summary>
    /// Contains extension methods for the IDependencyContainer interface.
    /// </summary>
    public static class DependencyContainerExtensions
    {
        [PublicAPI]
        public static DependencyInfo GetDependencyInfo([NotNull] this object o, [CanBeNull] Type t = null)
        {
            t ??= o.GetType();
            Assert.AreEqual(o.GetType(), t);

            return new(t, ConnectorReflectionUtils.GetAllConnectors(t));
        }

        [PublicAPI]
        public static void Inject([NotNull] this IDependencyContainer container, [NotNull] object o, [NotNull] DependencyInfo info)
        {
            Assert.AreEqual(o.GetType(), info.Type);

            foreach (var connectorType in info.ConnectorTypes)
            {
                Type providerType = connectorType.GetGenericArguments()[0];
                if (!container.TryGetProvider(providerType, out var p)) continue;

                ConnectorReflectionUtils.Connect(connectorType, o, p);
            }
        }

        [PublicAPI]
        public static void Detach([NotNull] this IDependencyContainer container, [NotNull] object o,
            [NotNull]                            DependencyInfo       info)
        {
            Assert.AreEqual(o.GetType(), info.Type);

            foreach (var connectorType in info.ConnectorTypes)
            {
                Type providerType = connectorType.GetGenericArguments()[0];
                if (!container.TryGetProvider(providerType, out var p)) continue;

                ConnectorReflectionUtils.Disconnect(connectorType, o, p);
            }
        }

        /// <summary>
        /// Injects dependencies into the given object using the dependency container.
        /// </summary>
        /// <param name="container">The dependency container.</param>
        /// <param name="o">The object to inject dependencies into.</param>
        /// <remarks>
        /// This method uses the dependency container to identify the connectors for the given object type.
        /// It then retrieves the corresponding providers from the container and connects them to the object using reflection.
        /// This allows the object to access the required dependencies through the connectors.
        /// </remarks>
        [PublicAPI]
        public static void Inject([NotNull] this IDependencyContainer container, [NotNull] object o)
        {
            const string debugName  = "DependencyContainerExtensions.Inject(object)";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            Type t = o.GetType();
            foreach (var connectorType in ConnectorReflectionUtils.GetAllConnectors(t))
            {
                Type providerType = connectorType.GetGenericArguments()[0];
                if (!container.TryGetProvider(providerType, out var p)) continue;

                ConnectorReflectionUtils.Connect(connectorType, o, p);
            }
        }

        /// <summary>
        /// Detaches the specified object from the dependency container.
        /// </summary>
        /// <param name="container">The dependency container.</param>
        /// <param name="o">The object to detach.</param>
        /// <remarks>
        /// This method is used to detach the specified object from the dependency container.
        /// It iterates through all connector types of the object and disconnects them from their corresponding providers in the container.
        /// </remarks>
        [PublicAPI]
        public static void Detach([NotNull] this IDependencyContainer container, [NotNull] object o)
        {
            const string debugName  = "DependencyContainerExtensions.Detach(object)";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            Type t = o.GetType();
            foreach (var connectorType in ConnectorReflectionUtils.GetAllConnectors(t))
            {
                Type providerType = connectorType.GetGenericArguments()[0];
                if (!container.TryGetProvider(providerType, out var p)) continue;

                ConnectorReflectionUtils.Disconnect(connectorType, o, p);
            }
        }

        /// <summary>
        /// Injects dependencies into the specified object.
        /// </summary>
        /// <param name="container">The dependency container to use for resolving dependencies.</param>
        /// <param name="go">The object to inject dependencies into.</param>
        /// <remarks>
        /// This method injects dependencies into the specified object by resolving them from the dependency container.
        /// The object's dependencies are identified by the [IConnector] attributes applied to its properties or fields.
        /// </remarks>
        [PublicAPI]
        public static void Inject([NotNull] this IDependencyContainer container, [NotNull] GameObject go)
        {
            const string debugName  = "DependencyContainerExtensions.Inject(GameObject)";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            if (go.TryGetComponent(out DependencyInjector injector))
            {
                Inject(container, injector);
                return;
            }

            foreach (var item in container.GetEnumerable())
            {
                InjectRecursive(container, go, item.Key, item.Value);
            }
        }

        /// <summary>
        /// Detaches the specified game object from the dependency container.
        /// </summary>
        /// <param name="container">The dependency container to detach from.</param>
        /// <param name="go">The game object to detach.</param>
        /// <remarks>
        /// This method detaches the specified game object from the dependency container. It is used to remove the game object from any registered connectors and disconnect any associated dependencies.
        /// If the game object has a <see cref="DependencyInjector"/> component attached, it will be detached using <see cref="Detach(IDependencyContainer, DependencyInjector)"/> method.
        /// Otherwise, the method iterates over all registered connectors in the container, and detaches the game object recursively for each connector type using <see cref="DetachRecursive(IDependencyContainer, GameObject, Type, IProvider)"/> method.
        /// </remarks>
        [PublicAPI]
        public static void Detach([NotNull] this IDependencyContainer container, [NotNull] GameObject go)
        {
            const string debugName  = "DependencyContainerExtensions.Detach(GameObject)";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            if (go.TryGetComponent(out DependencyInjector injector))
            {
                Detach(container, injector);
                return;
            }

            foreach (var item in container.GetEnumerable())
            {
                DetachRecursive(container, go, item.Key, item.Value);
            }
        }

        /// <summary>
        /// Injects the specified provider recursively into game objects that have a specified connector type.
        /// </summary>
        /// <param name="container">The dependency container.</param>
        /// <param name="go">The game object to inject into.</param>
        /// <param name="providerType">The type of the provider to inject.</param>
        /// <param name="provider">The provider to inject.</param>
        /// <remarks>
        /// This method is used to inject a provider into game objects recursively.
        /// It searches for game objects that have a dependency injector component and injects the provider into them.
        /// It also connects the connector of the specified type with the provider on each game object that has the connector component.
        /// </remarks>
        private static void InjectRecursive(
            [NotNull] IDependencyContainer container,
            [NotNull] GameObject           go,
            [NotNull] Type                 providerType,
            [NotNull] IProvider            provider)
        {
            const string debugName  = "DependencyContainerExtensions.InjectRecursive";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            var queue = new Queue<GameObject>(2);
            queue.Enqueue(go);

            while (queue.Count > 0)
            {
                GameObject current = queue.Dequeue();

                if (current.TryGetComponent(out DependencyInjector injector))
                {
                    Inject(container, injector);
                    continue;
                }

                var components = ListPool<Component>.Get();
                current.GetComponents(components);
                foreach (var com in components)
                {
                    var comT = com.GetType();
                    if (!ConnectorReflectionUtils.TryGetConnectorType(comT, providerType, out var connectorType))
                    {
                        continue;
                    }
                    ConnectorReflectionUtils.Connect(connectorType, com, provider);
                }
                components.Clear();
                ListPool<Component>.Release(components);

                foreach (Transform child in current.transform)
                {
                    queue.Enqueue(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Detaches the specified game object from the dependency container recursively.
        /// </summary>
        /// <param name="container">The dependency container to detach from.</param>
        /// <param name="go">The game object to detach.</param>
        /// <param name="providerType">The type of the provider.</param>
        /// <param name="provider">The instance of the provider.</param>
        /// <remarks>
        /// This method detaches the specified game object from the dependency container recursively. It is used to remove the game object from any registered connectors and disconnect any associated dependencies.
        /// If the game object has a DependencyInjector component attached, it will be detached using the Detach method.
        /// Otherwise, the method iterates over all registered connectors in the container, and detaches the game object recursively for each connector type using this method.
        /// </remarks>
        private static void DetachRecursive(
            [NotNull] IDependencyContainer container,
            [NotNull] GameObject           go,
            [NotNull] Type                 providerType,
            [NotNull] IProvider            provider)
        {
            const string debugName  = "DependencyContainerExtensions.DetachRecursive";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            var queue = new Queue<GameObject>(2);
            queue.Enqueue(go);

            while (queue.Count > 0)
            {
                GameObject current = queue.Dequeue();

                if (current.TryGetComponent(out DependencyInjector injector))
                {
                    Detach(container, injector);
                    continue;
                }

                var components = ListPool<Component>.Get();
                current.GetComponents(components);
                foreach (var com in components)
                {
                    var comT = com.GetType();
                    if (!ConnectorReflectionUtils.TryGetConnectorType(comT, providerType, out var connectorType))
                    {
                        continue;
                    }
                    ConnectorReflectionUtils.Disconnect(connectorType, com, provider);
                }
                components.Clear();
                ListPool<Component>.Release(components);

                foreach (Transform child in current.transform)
                {
                    queue.Enqueue(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Injects dependencies into the specified object or game object.
        /// </summary>
        /// <param name="container">The dependency container.</param>
        /// <param name="injector">The object to inject dependencies into.</param>
        /// <remarks>
        /// This method injects dependencies into the specified object or game object. It connects the object or game object with the appropriate providers contained in the dependency container.
        /// If the specified object is a game object, it first tries to find a `DependencyInjector` component attached to the game object. If found, it invokes the `Inject` method of the `DependencyInjector` to perform the injection.
        /// If the specified object is not a game object or does not have a `DependencyInjector`, it iterates over all registered dependencies in the container and performs the injection manually.
        /// </remarks>
        private static void Inject(
            [NotNull] IDependencyContainer container, [NotNull] DependencyInjector injector)
        {
            const string debugName  = "DependencyContainerExtensions.Inject(IDependencyContainer, DependencyInjector)";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            foreach (var item in container.GetEnumerable())
            {
                injector.Inject(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Detaches the specified game object from the dependency container.
        /// </summary>
        /// <param name="container">The dependency container to detach from.</param>
        /// <param name="injector">The game object to detach.</param>
        /// <remarks>
        /// This method detaches the specified game object from the dependency container. It is used to remove the game object from any registered connectors and disconnect any associated dependencies.
        /// If the game object has a <see cref="DependencyInjector"/> component attached, it will be detached using <see cref="Detach(IDependencyContainer, DependencyInjector)"/> method.
        /// Otherwise, the method iterates over all registered connectors in the container, and detaches the game object recursively for each connector type using <see cref="DetachRecursive(IDependencyContainer, GameObject, Type, IProvider)"/> method.
        /// </remarks>
        private static void Detach(
            [NotNull] IDependencyContainer container, [NotNull] DependencyInjector injector)
        {
            const string debugName  = "DependencyContainerExtensions.Detach(IDependencyContainer, DependencyInjector)";
            using var    debugTimer = DebugTimer.StartWithCustomName(debugName);

            foreach (var item in container.GetEnumerable())
            {
                injector.Detach(item.Key, item.Value);
            }
        }
    }
}