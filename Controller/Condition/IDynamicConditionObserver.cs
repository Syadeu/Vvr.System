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
using System.Buffers;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Vvr.Model;
using Vvr.Provider;

namespace Vvr.Controller.Condition
{
    public delegate UniTask ConditionObserverDelegate(IEventTarget owner, string value);

    public interface IDynamicConditionObserver : IDisposable
    {
        ConditionObserverDelegate this[Model.Condition t] { get; set; }
    }

    internal sealed class DynamicConditionObserver : IConditionObserver, IDynamicConditionObserver
    {
        public static readonly ConditionObserverDelegate None = (_, _) => UniTask.CompletedTask;

        private ConditionResolver m_Parent;

        private ConditionQuery              m_Filter;
        private ConditionObserverDelegate[] m_Delegates;

        public ConditionObserverDelegate this[Model.Condition t]
        {
            get
            {
                if (m_Delegates == null ||
                    !m_Filter.Has(t))
                {
                    return null;
                }

                int i = m_Filter.IndexOf(t);
                return m_Delegates[i];
            }
            set
            {
                var modifiedQuery  = m_Filter | t;
                int modifiedLength = modifiedQuery.MaxIndex + 1;

                // require resize
                if (m_Delegates == null || m_Delegates.Length < modifiedLength)
                {
                    var nArr = ArrayPool<ConditionObserverDelegate>.Shared.Rent(modifiedLength);

                    if (m_Delegates != null)
                    {
                        foreach (var condition in m_Filter)
                        {
                            nArr[modifiedQuery.IndexOf(condition)] = m_Delegates[m_Filter.IndexOf(condition)];
                        }

                        ArrayPool<ConditionObserverDelegate>.Shared.Return(m_Delegates, true);
                    }

                    m_Delegates = nArr;
                }

                if (!m_Filter.IsEmpty && (short)t < (short)m_Filter.First)
                {
                    for (int j = 0; j < m_Delegates.Length - 1; j++)
                    {
                        m_Delegates[j + 1] = m_Delegates[j];
                    }

                    m_Delegates[0] = value;
                    m_Filter       = modifiedQuery;
                    return;
                }

                m_Filter = modifiedQuery;
                int i = m_Filter.IndexOf(t);

                m_Delegates[i] = value;
            }
        }

        ConditionQuery IConditionObserver.Filter => m_Filter;

        internal DynamicConditionObserver(ConditionResolver r)
        {
            m_Parent = r;
        }

        async UniTask IConditionObserver.OnExecute(Model.Condition condition, string value)
        {
            if (m_Parent.Disposed)
                throw new ObjectDisposedException(nameof(ConditionResolver));

            int i = m_Filter.IndexOf(condition);
            Assert.IsFalse(i < 0);

            if (m_Delegates[i] == null)
                throw new InvalidOperationException($"{condition} not found with value({value}), {m_Filter.ToString()}");
;            await m_Delegates[i](m_Parent.Owner, value);
        }

        public void Dispose()
        {
            if (!m_Parent.Disposed)
                m_Parent.Unsubscribe(this);

            ArrayPool<ConditionObserverDelegate>.Shared.Return(m_Delegates, true);
            m_Parent = null;
        }
    }
}