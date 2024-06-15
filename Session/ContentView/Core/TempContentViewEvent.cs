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
// File created : 2024, 06, 16 02:06

#endregion

using System;

namespace Vvr.Session.ContentView.Core
{
    public readonly struct TempContentViewEvent<TEvent> : IDisposable
        where TEvent : struct, IConvertible
    {
        private readonly IContentViewEventHandler<TEvent> m_EventHandler;
        private readonly TEvent                           m_Event;
        private readonly ContentViewEventDelegate<TEvent> m_Action;

        public TempContentViewEvent(IContentViewEventHandler<TEvent> h, TEvent ev, ContentViewEventDelegate<TEvent> a)
        {
            m_EventHandler = h;
            m_Event        = ev;
            m_Action       = a;
        }
        public void Dispose()
        {
            m_EventHandler.Unregister(m_Event, m_Action);
        }
    }

    public static class TempContentViewEventExtensions
    {
        public static TempContentViewEvent<TEvent> Temp<TEvent>(this IContentViewEventHandler<TEvent> t, TEvent ev,
            ContentViewEventDelegate<TEvent>                                                          a)
        where TEvent : struct, IConvertible
        {
            t.Register(ev, a);
            return new TempContentViewEvent<TEvent>(t, ev, a);
        }
    }
}