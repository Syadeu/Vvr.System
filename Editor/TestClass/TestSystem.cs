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
// File created : 2024, 05, 21 00:05

#endregion

using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Vvr.Provider;
using Vvr.Session;
using Vvr.Session.World;

namespace Vvr.TestClass
{
    public abstract class TestSystem<TWorld> : MonoBehaviour
        where TWorld : class, IWorldSession, IParentSession
    {
        [SerializeField] private bool m_AutoDownload;

        private TWorld m_World;

        public TWorld World => m_World;

        private async UniTaskVoid Start()
        {
            if (m_AutoDownload)
                await TestUtils.DownloadSheet();

            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }

            m_World = await GameWorld.GetOrCreate<TWorld>(Owner.Issue);

            await OnStart(m_World);
        }

        protected abstract UniTask OnStart(TWorld world);

        private string  m_Result;
        private bool m_Downloading;

        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(m_Downloading))
            {
                string text;
                if (m_Downloading)
                    text = "Downloading...";
                else
                    text = "Download Excel sheets";
                if (GUILayout.Button(text))
                {
                    Download().Forget();
                }
            }

            if (!m_Result.IsNullOrEmpty())
            {
                EditorGUILayout.LabelField(m_Result);
            }
        }

        private async UniTaskVoid Download()
        {
            m_Downloading = true;
            try
            {
                await TestUtils.DownloadSheet();
                m_Result = null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                m_Result = e.Message;
            }
            finally
            {
                m_Downloading = false;
            }
        }

        // [Button]
        // private async void DownloadExcelSheets()
        // {
        //     await TestUtils.DownloadSheet();
        // }
    }
}