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
// File created : 2024, 05, 07 20:05

#endregion

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Vvr.Controller.Provider;
using Vvr.Controller.Stat;
using Vvr.Model;
using Vvr.Model.Stat;
using Vvr.Provider;

namespace Vvr.Controller.Item
{
    public sealed partial class ItemInventory : IItemProvider, IDisposable
    {
        private readonly IEventTarget m_Owner;
        private readonly IItem[]      m_Equipments = new IItem[6];
        private readonly List<IItem>  m_Items      = new();

        private bool Disposed { get; set; }

        public ItemInventory(IEventTarget owner)
        {
            m_Owner = owner;
        }
        public void Dispose()
        {
            Array.Clear(m_Equipments, 0, m_Equipments.Length);
            m_Items.Clear();

            Disposed = true;
        }

        public void Add(ItemSheet.Row item)
        {
            throw new NotImplementedException();
            // m_Items.Add(item);
        }

        public IItem Resolve(int index)
        {
            Assert.IsFalse(index < 0);
            Assert.IsTrue(index  < m_Equipments.Length);

            return m_Equipments[index];
        }
    }

    partial class ItemInventory : IStatModifier
    {
        private bool m_IsDirty;

        bool IStatModifier.IsDirty => m_IsDirty;
        int IStatModifier. Order   => StatModifierOrder.Item;

        void IStatModifier.UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
        {


            m_IsDirty = false;
        }
    }
}