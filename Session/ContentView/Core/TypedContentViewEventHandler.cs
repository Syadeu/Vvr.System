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
// File created : 2024, 06, 15 14:06

#endregion

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Session.ContentView.Core
{
    [PublicAPI]
    public class TypedContentViewEventHandler<TEvent>
        : ContentViewEventHandler<TEvent>, ITypedContentViewEventHandler<TEvent>
        where TEvent : struct, IConvertible
    {
        abstract class TypedExecutionBody : IDisposable
        {
            public abstract UniTask Execute(TEvent e, object v);

            public virtual void Dispose(){}
        }

        class TypedExecutionBody<TValue> : TypedExecutionBody
        {
            private readonly ContentViewEventDelegate<TEvent, TValue> m_Action;

            public TypedExecutionBody(ContentViewEventDelegate<TEvent, TValue> x)
            {
                m_Action = x;
            }
            public override UniTask Execute(TEvent e, object v)
            {
                return m_Action.Invoke(e, (TValue)v);
            }
        }

        private readonly Dictionary<uint, List<uint>>                   m_TypedActions   = new();
        private readonly ConcurrentDictionary<uint, TypedExecutionBody> m_TypedActionMap = new();

        private static uint CalculateKeyHash(TEvent e, Type valueType)
        {
            return unchecked((uint)e.GetHashCode()) ^ 267 ^ FNV1a32.Calculate(valueType.AssemblyQualifiedName);
        }
        private static uint CalculateTypedHash(
            string methodName,
            Type   declaringType, object target,
            Type   valueType)
        {
            uint hash =
                FNV1a32.Calculate(methodName)
                ^ 267;
            if (declaringType is not null)
                hash ^= FNV1a32.Calculate(declaringType.FullName);
            if (target is not null)
                hash ^= unchecked((uint)target.GetHashCode());

            hash ^= FNV1a32.Calculate(valueType.AssemblyQualifiedName);
            return hash;
        }

        public override void Clear()
        {
            base.Clear();

            using var writeLock = new WriteLock(this);
            writeLock.Wait(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return;

            foreach (var list in m_TypedActions.Values)
            {
                list.Clear();
            }
            foreach (var item in m_TypedActionMap.Values)
            {
                item.Dispose();
            }

            m_TypedActions.Clear();
            m_TypedActionMap.Clear();
        }

        public ITypedContentViewEventHandler<TEvent> Register<TValue>(TEvent e, ContentViewEventDelegate<TEvent, TValue> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(TypedContentViewEventHandler<TEvent>));

            using var writeLock = new WriteLock(this);
            writeLock.Wait(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return this;

            // Fully under consideration of retrieving type resolve is expansive.
            // However, we need to think about AOT problems
            // when this method calling from interfaces.
            Type valueType = typeof(TValue);
            uint keyHash   = CalculateKeyHash(e, valueType);
            if (m_TypedActions.TryGetValue(keyHash, out var list))
            {
                list                    = new(8);
                m_TypedActions[keyHash] = list;
            }

            uint hash = CalculateTypedHash(
                x.Method.Name, x.Method.DeclaringType, x.Target,
                valueType);
            if (list.Contains(hash))
                throw new InvalidOperationException("hash conflict possibly registering same method");

            m_TypedActionMap[hash] = new TypedExecutionBody<TValue>(x);
            list.Add(hash);

            return this;
        }

        public ITypedContentViewEventHandler<TEvent> Unregister<TValue>(TEvent e, ContentViewEventDelegate<TEvent, TValue> x)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(TypedContentViewEventHandler<TEvent>));

            using var writeLock = new WriteLock(this);
            writeLock.Wait(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return this;

            Type valueType = typeof(TValue);
            uint keyHash   = CalculateKeyHash(e, valueType);
            if (!m_TypedActions.TryGetValue(keyHash, out var list)) return this;

            uint hash = CalculateTypedHash(
                x.Method.Name, x.Method.DeclaringType, x.Target,
                valueType);
            if (!m_TypedActionMap.TryRemove(hash, out var item))
            {
                return this;
            }

            list.Remove(hash);
            item.Dispose();

            return this;
        }

        public override async UniTask ExecuteAsync(TEvent e, object ctx)
        {
            await base.ExecuteAsync(e, ctx);

            if (ctx == null || CancellationToken.IsCancellationRequested)
            {
                return;
            }

            ExecutionLock executionLock = new ExecutionLock(this);
            await executionLock.WaitAsync(CancellationToken);

            if (CancellationToken.IsCancellationRequested) return;

            Type valueType = ctx.GetType();
            uint keyHash   = CalculateKeyHash(e, valueType);
            if (!m_TypedActions.TryGetValue(keyHash, out var list)) return;

            int count = list.Count;
            var array = ArrayPool<UniTask>.Shared.Rent(count);

            int i = 0;
            for (; i < count; i++)
            {
                if (!m_TypedActionMap.TryGetValue(list[i], out var executionBody))
                {
                    continue;
                }

                array[i] = (executionBody.Execute(e, ctx))
                    .AttachExternalCancellation(CancellationToken);
            }

            for (; i < array.Length; i++)
            {
                array[i] = UniTask.CompletedTask;
            }

            executionLock.Dispose();

            await UniTask.WhenAll(array)
                    .AttachExternalCancellation(CancellationToken)
                ;
            ArrayPool<UniTask>.Shared.Return(array, true);
        }
    }
}