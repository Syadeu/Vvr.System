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
// File created : 2024, 06, 17 21:06

#endregion

using JetBrains.Annotations;
using Vvr.Controller.Condition;
using Vvr.Provider;

namespace Vvr.TestClass
{
    [PublicAPI]
    public class TestConditionTarget : TestEventTarget, IConditionTarget
    {
        public IReadOnlyConditionResolver ConditionResolver { get; set; }

        public TestConditionTarget(string displayName) : base(displayName)
        {
        }
        public TestConditionTarget(Owner owner, string displayName) : base(owner, displayName)
        {
        }
    }
}