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
// File created : 2024, 05, 09 21:05

#endregion

using Cysharp.Threading.Tasks;
using Vvr.Controller.Actor;
using Vvr.Model;

namespace Vvr.Controller.Skill
{
    public interface ISkill
    {
        UniTask Queue(SkillSheet.Row data);
        UniTask Queue(SkillSheet.Row data, IActor specifiedTarget);
    }
}