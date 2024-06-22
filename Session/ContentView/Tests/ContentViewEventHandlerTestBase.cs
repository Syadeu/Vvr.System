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
// File created : 2024, 06, 22 13:06

#endregion

using JetBrains.Annotations;
using NUnit.Framework;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Tests
{
    [PublicAPI]
    public abstract class ContentViewEventHandlerTestBase
    {
        public ContentViewEventHandler<TestContentViewEvent> EventHandler { get; private set; }

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            EventHandler = CreateEventHandler();
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            EventHandler.Dispose();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Assert.IsFalse(EventHandler.WriteLocked);
            EventHandler.Clear();
        }

        protected virtual ContentViewEventHandler<TestContentViewEvent> CreateEventHandler()
        {
            return new();
        }
    }

    [PublicAPI]
    public abstract class TypedContentViewEventHandlerTestBase : ContentViewEventHandlerTestBase
    {
        public new TypedContentViewEventHandler<TestContentViewEvent> EventHandler => (
            TypedContentViewEventHandler<TestContentViewEvent>)base.EventHandler;

        protected override ContentViewEventHandler<TestContentViewEvent> CreateEventHandler()
        {
            return new TypedContentViewEventHandler<TestContentViewEvent>();
        }
    }
}