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
// File created : 2024, 05, 29 10:05

#endregion

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Provider
{
    /// <summary>
    /// Represents a provider for canvas views.
    /// </summary>
    [LocalProvider, PublicAPI]
    public interface ICanvasViewProvider : IProvider
    {
        /// <summary>
        /// Resolves the overlay canvas view.
        /// </summary>
        /// <param name="sortOrder">The sort order of the canvas.</param>
        /// <param name="raycaster">Indicates whether a raycaster should be attached to the canvas.</param>
        /// <returns>The resolved overlay canvas view.</returns>
        IImmutableObject<UnityEngine.Canvas> ResolveOverlay(CanvasSortOrder sortOrder, bool raycaster);

        /// <summary>
        /// Resolves the camera canvas view.
        /// </summary>
        /// <param name="cameraType">The type of camera to be used.</param>
        /// <param name="sortingLayerName">The name of the sorting layer for the canvas.</param>
        /// <param name="sortOrder">The sort order of the canvas.</param>
        /// <param name="raycaster">Indicates whether a raycaster should be attached to the canvas.</param>
        /// <returns>The resolved camera canvas view.</returns>
        IImmutableObject<UnityEngine.Canvas> ResolveCamera(
            CanvasCameraType cameraType,
            CanvasLayerName  sortingLayerName, CanvasSortOrder sortOrder,
            bool             raycaster);
    }

    /// <summary>
    /// Represents the type of camera to be used for the canvas.
    /// </summary>
    public enum CanvasCameraType : short
    {
        Default = 0,
        UICamera
    }

    /// <summary>
    /// Represents the names of canvas layers.
    /// </summary>
    public enum CanvasLayerName : short
    {
        Background,
        CardUI,
        OverlayUI,
    }

    /// <summary>
    /// Represents the sort order of a canvas.
    /// </summary>
    public enum CanvasSortOrder : short
    {
    }
}