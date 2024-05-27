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
// File created : 2024, 05, 26 22:05

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;

namespace Vvr.Session.ContentView.Dialogue.Attributes
{
    static class DialogueAttributeHelper
    {
        private static readonly Dictionary<string, Type>
            s_AttributeTypeMap = new();
        private static          ValueDropdownList<string> s_Values;

        public static IReadOnlyDictionary<string, Type>
            AttributeTypeMap => s_AttributeTypeMap;

        static DialogueAttributeHelper()
        {
            foreach (var type in VvrTypeHelper.GetTypesIter(VvrTypeHelper.InheritsFrom<IDialogueAttribute>)
                         .Where(VvrTypeHelper.IsNotAbstract))
            {
                s_AttributeTypeMap[type.AssemblyQualifiedName] = type;
            }
        }

        internal static ValueDropdownList<string> GetDropdownList()
        {
            if (s_Values != null) return s_Values;

            s_Values = new ValueDropdownList<string>();
            foreach (var e in s_AttributeTypeMap)
            {
                var displayNameAtt = e.Value.GetCustomAttribute<DisplayNameAttribute>();
                if (displayNameAtt == null)
                    s_Values.Add(new ValueDropdownItem<string>(e.Value.Name, e.Key));
                else
                    s_Values.Add(new ValueDropdownItem<string>(displayNameAtt.DisplayName, e.Key));
            }

            return s_Values;
        }
    }
}