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
// File created : 2024, 06, 12 18:06

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vvr.Model
{
    [PublicAPI]
    public struct SemaphoreSlimLock : IDisposable
    {
        private readonly SemaphoreSlim m_SemaphoreSlim;

        private bool m_Started;

        public SemaphoreSlimLock(SemaphoreSlim s)
        {
            m_SemaphoreSlim = s;
            m_Started       = false;
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            if (m_Started)
                throw new InvalidOperationException();

            m_Started = true;
            return m_SemaphoreSlim.WaitAsync(cancellationToken);
        }

        public void Wait(CancellationToken cancellationToken)
        {
            if (m_Started)
                throw new InvalidOperationException();

            m_Started = true;
            m_SemaphoreSlim.Wait(cancellationToken);
        }

        public void Dispose()
        {
            if (!m_Started)
                throw new InvalidOperationException();

            m_SemaphoreSlim?.Release();
        }
    }
}