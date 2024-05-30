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
// File created : 2024, 05, 05 12:05

#endregion

using UnityEngine;

namespace Vvr.Model
{
    /// <summary>
    /// Delegate that represents a method implementation that takes in a float as the source value and another float as the parameter value, and returns a float.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The result of the method.</returns>
    public delegate float MethodImplDelegate(float source, float value);

    /// <summary>
    /// Represents a method implementation that can be applied to calculate a result based on a source value and a parameter value.
    /// </summary>
    public enum Method : short
    {
        Override = 0,

        Addictive,
        Subtract,

        Multiplier,
        Divide,

        AddMultiplier,
        SubMultiplier,
        AddDivide,
        SubDivide,

        Log,
    }

    /// <summary>
    /// Helper class for working with the Method enum and method delegates.
    /// </summary>
    public static class MethodHelper
    {
        public static readonly MethodImplDelegate
        // ReSharper disable MemberCanBePrivate.Global
            Override      = (_,      value) => value,
            Addictive     = (source, value) => source + value,
            Subtract      = (source, value) => source - value,
            Multiplier    = (source, value) => source * value,
            Divide        = (source, value) => source / value,
            AddMultiplier = (source, value) => source + source * value,
            SubMultiplier = (source, value) => source - source * value,
            AddDivide     = (source, value) => source + source / value,
            SubDivide     = (source, value) => source - source / value,
            Log           = Mathf.Log;
        // ReSharper restore MemberCanBePrivate.Global

        /// <summary>
        /// Converts the given Method enum value to its corresponding MethodImplDelegate delegate.
        /// </summary>
        /// <param name="method">The Method enum value to convert.</param>
        /// <returns>The MethodImplDelegate delegate corresponding to the given Method enum value.</returns>
        public static MethodImplDelegate ToDelegate(this Method method)
        {
            switch (method)
            {
                case Method.Addictive:
                    return Addictive;
                case Method.Subtract:
                    return Subtract;

                case Method.Multiplier:
                    return Multiplier;
                case Method.Divide:
                    return Divide;

                case Method.AddMultiplier:
                    return AddMultiplier;
                case Method.SubMultiplier:
                    return SubMultiplier;

                case Method.AddDivide:
                    return AddDivide;
                case Method.SubDivide:
                    return SubDivide;

                case Method.Log:
                    return Log;

                case Method.Override:
                default:
                    return Override;
            }
        }
    }
}