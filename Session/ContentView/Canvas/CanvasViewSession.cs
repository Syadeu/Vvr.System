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
// File created : 2024, 05, 29 09:05
#endregion

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Vvr.Model;
using Vvr.Provider;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Canvas
{
    /// <summary>
    /// Represents a canvas view session for displaying canvas elements.
    /// </summary>
    [UsedImplicitly]
    public sealed class CanvasViewSession : ParentSession<CanvasViewSession.SessionData>,
        ICanvasViewProvider,
        IConnector<ICanvasCameraProvider>
    {
        public static readonly Vector2Int DefaultResolution = new Vector2Int(2532, 1080);

        public struct SessionData : ISessionData
        {
            public Vector2Int? referenceResolution;
        }

        class ImmutableCanvas : IImmutableObject<UnityEngine.Canvas>
        {
            Object IImmutableObject.  Object => Object;

            public UnityEngine.Canvas Object { get; }

            public ImmutableCanvas(UnityEngine.Canvas c)
            {
                Object = c;
            }
        }

        public override string DisplayName => nameof(CanvasViewSession);

        private          Transform                        m_CanvasParent;
        private readonly Dictionary<int, ImmutableCanvas> m_CanvasMap = new();

        private ICanvasCameraProvider m_CameraProvider;

        protected override async UniTask OnInitialize(IParentSession session, SessionData data)
        {
            await base.OnInitialize(session, data);

            GameObject obj = new GameObject(nameof(CanvasViewSession));
#if UNITY_EDITOR
            obj.hideFlags = HideFlags.NotEditable;
#endif
            m_CanvasParent = obj.transform;

            Vvr.Provider.Provider.Static.Connect<ICanvasCameraProvider>(this);
        }

        protected override UniTask OnReserve()
        {
            Vvr.Provider.Provider.Static.Disconnect<ICanvasCameraProvider>(this);

            return base.OnReserve();
        }

        private void               CreateCanvas(string objName,
            CanvasSortOrder        sortOrder, bool                   raycast,
            out UnityEngine.Canvas canvas)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(m_CanvasParent);

            canvas = obj.AddComponent<UnityEngine.Canvas>();
            canvas.sortingOrder = (short)sortOrder;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                Data.referenceResolution ?? DefaultResolution;
            if (raycast)
            {
                var raycaster = obj.AddComponent<GraphicRaycaster>();
                raycaster.blockingMask           = LayerMask.GetMask("UI");
                raycaster.blockingObjects        = GraphicRaycaster.BlockingObjects.TwoD;
                raycaster.ignoreReversedGraphics = true;
            }
        }

        public IImmutableObject<UnityEngine.Canvas> ResolveOverlay(
            CanvasSortOrder sortOrder, bool raycaster)
        {
            int h = (short)sortOrder ^ raycaster.ToByte() ^ 37 ^ 267;

            if (m_CanvasMap.TryGetValue(h, out var v)) return v;

            string nameFormat       = $"Overlay {(short)sortOrder}";
            if (raycaster) nameFormat += " Raycast";

            CreateCanvas(nameFormat, sortOrder, raycaster, out var canvas);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var result = new ImmutableCanvas(canvas);
            m_CanvasMap[h] = result;
            return result;
        }
        public IImmutableObject<UnityEngine.Canvas> ResolveCamera(
            CanvasCameraType cameraType, RenderMode renderMode,
            CanvasLayerName sortingLayerName, CanvasSortOrder sortOrder, bool raycaster)
        {
            int h = (short)sortingLayerName ^ (short)sortOrder ^ raycaster.ToByte() ^ 37 ^ 267
                ^ (int)renderMode;

            if (m_CanvasMap.TryGetValue(h, out var v)) return v;

            string nameFormat         = $"Camera {renderMode} {(short)sortOrder}";
            if (raycaster) nameFormat += " Raycast";

            CreateCanvas(nameFormat, sortOrder, raycaster, out var canvas);
            canvas.renderMode = renderMode;
            if (cameraType == CanvasCameraType.UICamera)
                canvas.worldCamera = m_CameraProvider.UICamera;
            else
                canvas.worldCamera = m_CameraProvider.Default;

            canvas.sortingLayerName
                = VvrTypeHelper.Enum<CanvasLayerName>.ToString(sortingLayerName);

            if (renderMode == RenderMode.WorldSpace)
            {
                RectTransform rectTr = (RectTransform)canvas.transform;
                rectTr.localPosition = new Vector3(0, 0, 100);
                rectTr.sizeDelta =
                    new Vector2(Screen.width, Screen.height);
                rectTr.localScale    = Vector3.one * .17f;
            }

            var result = new ImmutableCanvas(canvas);
            m_CanvasMap[h] = result;
            return result;
        }

        void IConnector<ICanvasCameraProvider>.Connect(ICanvasCameraProvider    t) => m_CameraProvider = t;
        void IConnector<ICanvasCameraProvider>.Disconnect(ICanvasCameraProvider t) => m_CameraProvider = null;
    }
}