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
// File created : 2024, 06, 16 01:06

#endregion

namespace Vvr.Session.ContentView.Core
{
    public struct ModalView00OpenContext : IModalViewContext
    {
        public int ModalType => 0;

        public readonly string title;
        public readonly string description;
        public readonly bool   enableCancel;
        public readonly bool   enableConfirm;

        public ModalView00OpenContext(
            string title, string description, bool enableCancel, bool enableConfirm)
        {
            this.title         = title;
            this.description   = description;
            this.enableCancel  = enableCancel;
            this.enableConfirm = enableConfirm;
        }
    }
}