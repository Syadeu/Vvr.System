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
// File created : 2024, 05, 23 01:05

#endregion

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Vvr.Model
{
    /// <summary>
    /// Represents an unresolved custom method.
    /// </summary>
    public sealed class UnresolvedCustomMethod : IUnresolvedCustomMethod
    {
        /// <summary>
        /// Represents an abstract base element in the unresolved custom method.
        /// </summary>
        private abstract class Element
        {
            public readonly string rawValue;

            protected Element(string r)
            {
                rawValue = r;
            }
        }

        /// <summary>
        /// Represents a variable within an unresolved custom method.
        /// </summary>
        private abstract class Variable : Element
        {
            protected Variable(string r) : base(r)
            {

            }

            /// <summary>
            /// Resolves an unresolved custom method.
            /// </summary>
            /// <param name="resolver">The method argument resolver used to resolve method arguments.</param>
            /// <returns>The resolved result of the custom method.</returns>
            public abstract float Resolve(IMethodArgumentResolver resolver);
        }

        /// <summary>
        /// Represents a variable within an unresolved custom method.
        /// </summary>
        private sealed class RawValue : Variable
        {
            private readonly float m_Value;

            public RawValue(string r, float v) : base(r)
            {
                m_Value = v;
            }

            public override float Resolve(IMethodArgumentResolver resolver) => m_Value;
        }

        /// <summary>
        /// Represents a variable within an unresolved custom method that references another value.
        /// </summary>
        private sealed class UnresolvedReferenceValue : Variable
        {
            private readonly string m_Reference;

            public UnresolvedReferenceValue(string r, string re) : base(r)
            {
                m_Reference = re;
            }

            public override float Resolve(IMethodArgumentResolver resolver) => resolver.Resolve(m_Reference);
        }

        /// <summary>
        /// Represents a method value in an unresolved custom method.
        /// </summary>
        /// <remarks>
        /// This class is a derived class of the abstract base element <see cref="Element"/>.
        /// It represents a method value in an unresolved custom method.
        /// Method values are used in custom calculations to perform mathematical operations.
        /// </remarks>
        private sealed class MethodValue : Element
        {
            private readonly MethodImplDelegate m_Method;
            public readonly short              methodType;

            public MethodValue(string r, Method m) : base(r)
            {
                m_Method     = m.ToDelegate();
                methodType = (short)((short)m < (short)Method.Multiplier ? 0 : 1);
            }

            public float Resolve(float prev, float next)
            {
                return m_Method(prev, next);
            }
        }

        private readonly List<Element> m_Elements;

        public UnresolvedCustomMethod(CustomMethodSheet.Row row)
        {
            Assert.IsNotNull(row);

            m_Elements = ParseElements(row);
        }

        /// <summary>
        /// Parses the elements of an unresolved custom method.
        /// </summary>
        /// <param name="row">The row containing the custom method information.</param>
        /// <returns>A list of parsed elements representing the unresolved custom method.</returns>
        private static List<Element> ParseElements(CustomMethodSheet.Row row)
        {
            List<Element> elements = new();

            Dictionary<string, string> refValues = new();
            foreach (var e in row)
            {
                refValues[e.Name] = e.Value;
            }

            bool wasMethod = true;
            foreach (string entry in row.Calculations)
            {
                if (wasMethod)
                {
                    if (refValues.TryGetValue(entry, out var refVal))
                    {
                        if (!float.TryParse(refVal, out float v))
                            elements.Add(new UnresolvedReferenceValue(entry, refVal));
                        else
                            elements.Add(new RawValue(entry, v));
                    }
                    else
                        elements.Add(new RawValue(entry, float.Parse(entry)));

                    wasMethod = false;
                    continue;
                }

                Method m = VvrTypeHelper.Enum<Method>.ToEnum(entry);
                elements.Add(new MethodValue(entry, m));
                wasMethod = true;
            }

            return elements;
        }

        public float Execute(IMethodArgumentResolver resolver)
        {
            using DebugTimer t = DebugTimer.Start();

            if (m_Elements.Count < 2 && m_Elements[0] is Variable firstVar)
            {
                return firstVar.Resolve(resolver);
            }

            Stack<float>       resolvedValues = new();
            Stack<MethodValue> methods        = new();

            foreach (var element in m_Elements)
            {
                switch (element)
                {
                    case Variable v:
                        resolvedValues.Push(v.Resolve(resolver));
                        break;
                    case MethodValue m:
                        while (methods.TryPeek(out var e) &&
                               e.methodType >= m.methodType)
                        {
                            float operand2 = resolvedValues.Pop();
                            float operand1 = resolvedValues.Pop();
                            float result   = methods.Pop().Resolve(operand1, operand2);
                            resolvedValues.Push(result);
                        }

                        methods.Push(m);
                        break;
                }
            }

            while (methods.TryPop(out var method))
            {
                if (resolvedValues.Count < 2)
                    throw new InvalidOperationException("Variable is not enough");

                float operand2 = resolvedValues.Pop();
                float operand1 = resolvedValues.Pop();
                float result   = method.Resolve(operand1, operand2);
                resolvedValues.Push(result);
            }

            return resolvedValues.Pop();
        }
    }
}