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
// File created : 2024, 06, 13 16:06

#endregion

using UnityEngine;

namespace Vvr.Session.ContentView.Core
{
    public struct DeckViewSetActorContext
    {
        public int    index;
        public string id;

        public Sprite portrait;
        public int    grade;
        public int    level;

        public override int GetHashCode()
        {
            return
                unchecked((int)FNV1a32.Calculate(id))
                ^ index ^ grade ^ level ^ 367;
        }
    }
}