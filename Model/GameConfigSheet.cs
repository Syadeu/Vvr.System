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
// File created : 2024, 05, 10 10:05

#endregion

using System;
using System.Collections.Generic;
using Cathei.BakingSheet;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace Vvr.Model
{
    [Preserve]
    public sealed class GameConfigSheet : Sheet<GameConfigSheet.Row>
    {
        [Flags]
        public enum Target : short
        {
            ERROR = 0,

            Ally  = 0b010,
            Enemy = 0b100,
            Both  = 0b011,
        }

        [Flags]
        public enum Position : short
        {
            Default  = 0,
            Forward  = 0b01,
            Backward = 0b10,
            All      = 0b11
        }

        public struct Definition
        {
            [UsedImplicitly] public Target   Target   { get; private set; }
            [UsedImplicitly] public Position Position { get; private set; }
        }
        public struct Lifecycle
        {
            [UsedImplicitly] public MapType        Map       { get; private set; }
            [UsedImplicitly] public Condition Condition { get; private set; }
            [UsedImplicitly] public string         Value     { get; private set; }
        }
        public struct Evaluation
        {
            [UsedImplicitly] public Condition Condition   { get; private set; }
            [UsedImplicitly] public string    Value       { get; private set; }
            [UsedImplicitly] public float     Probability { get; private set; }
            [UsedImplicitly] public int       MaxCount    { get; private set; }
        }
        public struct Execution
        {
            [UsedImplicitly] public Condition  Condition { get; private set; }
            [UsedImplicitly] public string     Value     { get; private set; }
            [UsedImplicitly] public float      Delay     { get; private set; }
            [UsedImplicitly] public GameMethod Method    { get; private set; }
        }
        public sealed class Row : SheetRow
        {
            [UsedImplicitly] public Definition   Definition { get; private set; }
            [UsedImplicitly] public Lifecycle    Lifecycle  { get; private set; }
            [UsedImplicitly] public Evaluation   Evaluation { get; private set; }
            [UsedImplicitly] public Execution    Execution  { get; private set; }
            [UsedImplicitly] public List<string> Parameters { get; private set; }
        }

        [UsedImplicitly]
        public GameConfigSheet()
        {
            Name = nameof(GameDataSheets.GameConfigTable);
        }

        private readonly Dictionary<MapType, LinkedList<Row>> m_Configs = new();

        public override void PostLoad(SheetConvertingContext context)
        {
            base.PostLoad(context);

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            foreach (var item in this)
            {
                if (!m_Configs.TryGetValue(item.Lifecycle.Map, out var list))
                {
                    list                          = new();
                    m_Configs[item.Lifecycle.Map] = list;
                }

                list.AddLast(item);
            }
        }

        public IEnumerable<Row> this[MapType t]
        {
            get
            {
                if (!m_Configs.TryGetValue(t, out var list))
                {
                    return Array.Empty<Row>();
                }

                return list;
            }
        }
    }
}