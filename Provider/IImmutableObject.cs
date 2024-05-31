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
// File created : 2024, 05, 17 02:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Vvr.Provider
{
    /// <summary>
    /// Represents an immutable object.
    /// </summary>
    public interface IImmutableObject
    {
        /// <summary>
        /// This object is reference only. Asset modification is not allowed.
        /// </summary>
        [PublicAPI]
        UnityEngine.Object Object { get; }
    }

    /// <summary>
    /// Represents an immutable object.
    /// </summary>
    public interface IImmutableObject<out TObject> : IImmutableObject
        where TObject : UnityEngine.Object
    {
        /// <summary>
        /// This object is reference only. Asset modification is not allowed.
        /// </summary>
        [PublicAPI]
        new TObject Object { get; }
    }

    public static class ImmutableObjectExtensions
    {
        public static TObject CreateInstance<TObject>(this IImmutableObject<TObject> t)
            where TObject : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate(t.Object);
        }

        public static IImmutableObject<UnityEngine.Canvas> SetChild(
            this IImmutableObject<UnityEngine.Canvas> t, UnityEngine.Component com)
        {
            com.transform.SetParent(t.Object.transform, false);
            return t;
        }
        public static IImmutableObject<UnityEngine.Canvas> SetChild(
            this IImmutableObject<UnityEngine.Canvas> t, GameObject obj)
        {
            obj.transform.SetParent(t.Object.transform, false);
            return t;
        }
        public static TObject CreateChild<TObject>(
            this IImmutableObject<UnityEngine.Canvas> t,
            TObject                                template)
            where TObject : UnityEngine.Object
        {
            var ins = UnityEngine.Object.Instantiate(template, t.Object.transform, false);

            return ins;
        }
    }
}