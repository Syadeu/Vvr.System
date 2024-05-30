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
// File created : 2024, 05, 29 11:05

#endregion

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Core
{
    /// <summary>
    /// Represents an abstract child session that is used in a content view and can be managed by a parent session.
    /// </summary>
    /// <typeparam name="TSessionData">The type of session data stored in the child session.</typeparam>
    public abstract class ContentViewChildSession<TSessionData> : ParentSession<TSessionData>,
        IConnector<ICanvasViewProvider>

        where TSessionData : ISessionData
    {
        /// <summary>
        /// Represents a provider for accessing a canvas view.
        /// </summary>
        protected ICanvasViewProvider CanvasViewProvider { get; private set; }

        void IConnector<ICanvasViewProvider>.Connect(ICanvasViewProvider t) => CanvasViewProvider = t;
        void IConnector<ICanvasViewProvider>.Disconnect(ICanvasViewProvider t) => CanvasViewProvider = null;
    }
}