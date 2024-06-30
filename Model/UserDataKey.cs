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
// File created : 2024, 05, 24 22:05

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Vvr.Model
{
#pragma warning disable CS0660, CS0661
    public readonly ref struct UserDataKey
    {
        public struct Enumerable : IEnumerator<int>
        {
            private readonly string m_Key;
            private          int    m_Current;

            public int         Current => m_Current;
            object IEnumerator.Current => m_Current;

            public Enumerable(string k)
            {
                m_Key     = k;
                m_Current = 0;
            }
            public void Dispose()
            {
                // TODO release managed resources here
            }

            public bool        MoveNext()
            {
                if (m_Key.Length <= m_Current) return false;
                if (m_Current    != 0) m_Current++;

                int next = m_Key.IndexOf(UserDataPath.Delimiter, m_Current);
                if (next < 0)
                    m_Current  = m_Key.Length;
                else m_Current = next;

                return true;
            }

            public void        Reset()
            {
                m_Current = 0;
            }
        }

        private readonly string m_Key;

        // public int Count
        // {
        //     get
        //     {
        //         int i = 1, x = 0;
        //         while (x < m_Key.Length)
        //         {
        //             int index = m_Key.IndexOf(UserDataKeyCollection.Delimiter, x);
        //             if (index < 0) break;
        //
        //             i++;
        //             x = index + 1;
        //         }
        //
        //         return i;
        //     }
        // }

        [UsedImplicitly]
        private UserDataKey(string k)
        {
            m_Key = k;
        }

        public ReadOnlySpan<char> Read(int from, int end)
        {
            Assert.IsFalse(from < 0);
            Assert.IsFalse(end < 0);
            Assert.IsTrue(from < end);

            ReadOnlySpan<char> span = m_Key.AsSpan();

            var e = span[from..end];
            return e;
        }

        public Enumerable GetEnumerator() => new Enumerable(m_Key);

        public override int GetHashCode() => unchecked((int)FNV1a32.Calculate(m_Key)) ^ 267;
        public override string ToString() => m_Key;

        public static implicit operator UserDataKey(string t) => new(t);

        public static bool operator ==(UserDataKey x, string y) => x.m_Key == y;
        public static bool operator !=(UserDataKey x, string y) => !(x == y);
    }
#pragma warning restore CS0660, CS0661
}