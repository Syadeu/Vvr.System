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

using System;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [SheetValueConverter(typeof(DynamicReferenceConverter))]
    public struct DynamicReference : IEquatable<DynamicReference>
    {
        [UsedImplicitly]
        public string Id { get; private set; }

        public object Reference { get; private set; }

        public void SetReference(object o) => Reference = o;

        public static explicit operator DynamicReference(string t) => new DynamicReference() { Id = t };

        public bool Equals(DynamicReference other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is DynamicReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }

    [Preserve]
    internal class DynamicReferenceConverter : SheetValueConverter<DynamicReference>
    {
        protected override DynamicReference StringToValue(Type type, string value, SheetValueConvertingContext context)
        {
            return (DynamicReference)value;
        }
        protected override string ValueToString(Type type, DynamicReference value, SheetValueConvertingContext context)
        {
            return value.Id;
        }
    }

    public static class DynamicReferenceExtensions
    {
        public static TSheetRow Resolve<TSheetRow>(this DynamicReference t, ISheet<string, TSheetRow> sheet)
            where TSheetRow : ISheetRow
        {
            return sheet[t.Id];
        }
    }
}