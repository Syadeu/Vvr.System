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
// File created : 2024, 06, 23 19:06

#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Model;

namespace Vvr.Controller.Abnormal
{
    [PublicAPI]
    public readonly struct AbnormalHandle : IRawDataId, IUniqueID, IDisposable
    {
        private IAbnormal Owner    { get; }

        public string    Id       { get; }
        public UniqueID  UniqueID { get; }

        public bool Disposed => !Owner.Contains(this);

        public bool IsActivated
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(AbnormalHandle));
                return Owner.IsActivated(this);
            }
        }

        internal AbnormalHandle(IAbnormal owner, string id, UniqueID uniqueID)
        {
            Owner    = owner;
            Id       = id;
            UniqueID = uniqueID;
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(AbnormalHandle) + $" {Id}");

            Owner.RemoveAsync(this)
                .Forget();
        }
    }
}