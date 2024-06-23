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
// File created : 2024, 06, 23 19:06

#endregion

using System;
using JetBrains.Annotations;

namespace Vvr.Model
{
    [PublicAPI]
    public readonly struct UniqueID : IEquatable<UniqueID>, IComparable<UniqueID>
    {
        public static UniqueID Issue => new UniqueID(unchecked((int)FNV1a32.Calculate(Guid.NewGuid())));

        public readonly int value;

        public UniqueID(int v)
        {
            value = v;
        }

        public int CompareTo(UniqueID other) => value.CompareTo(other.value);
        public bool Equals(UniqueID other) => value == other.value;
        public override bool Equals(object obj) => obj is UniqueID other && Equals(other);
        public override int GetHashCode() => value;

        public static implicit operator int(UniqueID                t) => t.value;
        public static explicit operator UniqueID(int                t) => new UniqueID(t);
        public static implicit operator UniqueID(UnityEngine.Object t) => new(t.GetInstanceID());

        public static bool operator ==(UniqueID x, UniqueID y)
        {
            return x.value == y.value;
        }
        public static bool operator !=(UniqueID x, UniqueID y)
        {
            return !(x == y);
        }


    }

    [PublicAPI]
    public interface IUniqueID
    {
        UniqueID UniqueID { get; }
    }
}