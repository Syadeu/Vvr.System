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
// File created : 2024, 05, 11 17:05

#endregion

using System;
using System.Collections.Generic;
using Vvr.Model;

namespace Vvr.Provider
{
    public interface IGameConfigProvider : IProvider
    {
        IEnumerable<GameConfigSheet.Row> this[MapType t] { get; }
    }

    public sealed class GameConfigProvider : IGameConfigProvider
    {
        public static GameConfigProvider Construct(GameConfigSheet sheet)
        {
            Dictionary<MapType, LinkedList<GameConfigSheet.Row>> configs = new();
            foreach (var item in sheet)
            {
                if (!configs.TryGetValue(item.Lifecycle.Map, out var list))
                {
                    list                        = new();
                    configs[item.Lifecycle.Map] = list;
                }

                list.AddLast(item);
            }

            return new GameConfigProvider(configs);
        }

        private readonly Dictionary<MapType, LinkedList<GameConfigSheet.Row>> m_Configs;

        IEnumerable<GameConfigSheet.Row> IGameConfigProvider.this[MapType t]
        {
            get
            {
                if (!m_Configs.TryGetValue(t, out var list))
                {
                    return Array.Empty<GameConfigSheet.Row>();
                }

                return list;
            }
        }

        private GameConfigProvider(Dictionary<MapType, LinkedList<GameConfigSheet.Row>> c)
        {
            m_Configs = c;
        }
    }
}