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
// File created : 2024, 05, 10 12:05

#endregion

using System;

namespace Vvr.Controller.Session
{
    /// <summary>
    /// RootSession is an abstract class that serves as the base class for all root session implementations in the Vvr.Controller.Session namespace.
    /// </summary>
    public abstract class RootSession
        : ParentSession<RootSession.RootData>, IDisposable
    {
        public struct RootData : ISessionData {}

        public virtual void Dispose()
        {
        }
    }
}