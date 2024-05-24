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
// File created : 2024, 05, 23 22:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using Vvr.Provider;

namespace Vvr.Session.ContentView
{
    public delegate UniTask ContentViewEventDelegate<in TEvent>(TEvent e) where TEvent : struct, IConvertible;

    public interface IContentViewEventHandler<TEvent> : IContentViewEventHandler
        where TEvent : struct, IConvertible
    {
        IContentViewEventHandler<TEvent> Register(TEvent   e, ContentViewEventDelegate<TEvent> x);
        IContentViewEventHandler<TEvent> Unregister(TEvent e, ContentViewEventDelegate<TEvent> x);

        UniTask Execute(TEvent e);
    }

    [LocalProvider]
    public interface IContentViewEventHandler : IProvider, IDisposable
    {
    }
}