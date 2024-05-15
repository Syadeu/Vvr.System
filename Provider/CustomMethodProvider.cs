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
// File created : 2024, 05, 15 13:05
#endregion

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Controller.CustomMethod
{
    public class CustomMethodProvider : ICustomMethodProvider
    {
        public static CustomMethodProvider Static { get; private set; }

        public static CustomMethodProvider GetOrCreate(CustomMethodSheet sheet)
        {
            Static ??= new CustomMethodProvider(sheet);
            return Static;
        }

        abstract class Element
        {
            public readonly string rawValue;

            protected Element(string r)
            {
                rawValue = r;
            }
        }
        abstract class Variable : Element
        {
            public Variable(string r) : base(r)
            {

            }
            public abstract float Resolve(IReadOnlyStatValues stats);
        }

        class RawValue : Variable
        {
            public readonly float value;

            public RawValue(string r, float v) : base(r)
            {
                value = v;
            }

            public override float Resolve(IReadOnlyStatValues stats) => value;
        }
        class StatReferenceValue : Variable
        {
            public readonly StatType valueReference;

            public StatReferenceValue(string r, StatType v) : base(r)
            {
                valueReference = v;
            }

            public override float Resolve(IReadOnlyStatValues stats) => stats[valueReference];
        }

        class MethodValue : Element
        {
            public readonly MethodImplDelegate method;
            public readonly short             methodType;

            public MethodValue(string r, Method m) : base(r)
            {
                method     = m.ToDelegate();
                methodType = (short)((short)m < (short)Method.Multiplier ? 0 : 1);
            }

            public float Resolve(IReadOnlyStatValues stats, float prev, float next)
            {
                return method(prev, next);
            }
        }

        class MethodBody : List<Element>
        {
            public float Execute(IReadOnlyStatValues stats)
            {
                using DebugTimer t = DebugTimer.Start();

                if (Count < 2 && this[0] is Variable firstVar)
                {
                    return firstVar.Resolve(stats);
                }

                Stack<float>       resolvedValues = new();
                Stack<MethodValue> methods        = new();

                foreach (var element in this)
                {
                    switch (element)
                    {
                        case Variable v:
                            resolvedValues.Push(v.Resolve(stats));
                            break;
                        case MethodValue m:
                            while (methods.TryPeek(out var e) &&
                                   e.methodType >= m.methodType)
                            {
                                float operand2 = resolvedValues.Pop();
                                float operand1 = resolvedValues.Pop();
                                float result   = methods.Pop().Resolve(stats, operand1, operand2);
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
                    float result   = method.Resolve(stats, operand1, operand2);
                    resolvedValues.Push(result);
                }

                return resolvedValues.Pop();
            }
        }

        private CustomMethodSheet m_Sheet;

        private readonly Dictionary<int, MethodBody> m_Methods = new();

        private CustomMethodProvider(CustomMethodSheet t)
        {
            m_Sheet = t;
        }

        private MethodBody Create(CustomMethodSheet.Row row)
        {
            Assert.IsNotNull(row);
            Assert.IsNotNull(StatProvider.Static);
            MethodBody elements  = new();

            Dictionary<string, DynamicReference> refValues = new();
            foreach (var e in row)
            {
                refValues[e.Name] = e.Value;
            }

            bool       wasMethod = true;
            foreach (string entry in row.Calculations)
            {
                if (wasMethod)
                {
                    if (refValues.TryGetValue(entry, out var refVal))
                    {
                        if (StatProvider.Static.TryGetType(refVal.Id, out var statType))
                            elements.Add(new StatReferenceValue(entry, statType));
                        else
                            throw new NotImplementedException();
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

        public float Resolve(IReadOnlyStatValues stats, CustomMethodNames method)
        {
            int hash = method.GetHashCode();
            if (!m_Methods.TryGetValue(hash, out var body))
            {
                body              = Create(m_Sheet[method.ToString()]);
                m_Methods[hash] = body;
            }

            float result = body.Execute(stats);
            return result;
        }
    }

    public interface ICustomMethodProvider
    {
        [PublicAPI]
        float Resolve(IReadOnlyStatValues stats, CustomMethodNames method);
    }
}