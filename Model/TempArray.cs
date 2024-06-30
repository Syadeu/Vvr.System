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
// File created : 2024, 06, 12 17:06

#endregion

using System;
using System.Buffers;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Model
{
    [PublicAPI]
    public struct TempArray<T> : IDisposable
    {
        public static TempArray<T> Shared(int minLength, bool clearOnDispose = true)
        {
            if (minLength == 0)
                return new TempArray<T>(null, false, Array.Empty<T>());

            ArrayPool<T> pool = ArrayPool<T>.Shared;
            return new TempArray<T>(
                pool,
                clearOnDispose,
                pool.Rent(minLength)
            );
        }

        private readonly ArrayPool<T> m_Pool;
        private readonly bool         m_ClearOnDispose;

        public T[] Value { get; private set; }

        public T this[int index]
        {
            get => Value[index];
            set => Value[index] = value;
        }

        private TempArray(
            ArrayPool<T> pool,
            bool         clearOnDispose,
            T[]          arr
        )
        {
            m_Pool           = pool;
            m_ClearOnDispose = clearOnDispose;

            Value = arr;
        }
        public void Dispose()
        {
            if (m_Pool is not null)
                m_Pool.Return(Value, m_ClearOnDispose);
            Value = null;
        }

        public static implicit operator T[](TempArray<T> t) => t.Value;
    }
}