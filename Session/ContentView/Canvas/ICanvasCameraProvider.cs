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

using UnityEngine;
using Vvr.Provider;

namespace Vvr.Session.ContentView.Canvas
{
    /// <summary>
    /// Represents a canvas camera provider interface.
    /// </summary>
    public interface ICanvasCameraProvider : IProvider
    {
        /// <summary>
        /// Represents the default camera used in the canvas.
        /// </summary>
        /// <remarks>
        /// The Default property is used in the CanvasCameraProviderComponent class to provide access to the default camera.
        /// The camera is used for rendering canvas elements.
        /// </remarks>
        Camera Default  { get; }

        /// <summary>
        /// Represents the UI camera used in the canvas.
        /// </summary>
        /// <remarks>
        /// The UICamera property is used in the CanvasCameraProviderComponent class to provide access to the UI camera.
        /// The camera is used for rendering UI elements on the canvas.
        /// </remarks>
        Camera UICamera { get; }
    }
}