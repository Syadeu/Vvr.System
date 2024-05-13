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
// File created : 2024, 05, 13 15:05

#endregion

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.System.Model;

namespace Vvr.System.Controller
{
    public delegate UniTask ConditionObserverDelegate(string value);

    public interface IDynamicConditionObserver : IConditionObserver, IDisposable
    {
        ConditionObserverDelegate this[Condition t] { get; set; }
    }

    internal sealed class DynamicConditionObserver : IDynamicConditionObserver
    {
        private ConditionQuery                  m_Filter;
        private List<ConditionObserverDelegate> m_Delegates = new();

        public ConditionObserverDelegate this[Condition t]
        {
            get
            {
                int i = m_Filter.IndexOf(t);
                if (i < 0) return null;

                return m_Delegates[i];
            }
            set
            {
                m_Filter |= t;

                m_Delegates.Insert(m_Filter.IndexOf(t), value);
            }
        }

        ConditionQuery IConditionObserver.Filter => m_Filter;


        public async UniTask OnExecute(Condition condition, string value)
        {
            int i = m_Filter.IndexOf(condition);
            if (i < 0) return;

            await m_Delegates[i](value);
        }

        public void Dispose()
        {
            m_Delegates.Clear();
        }
    }
}