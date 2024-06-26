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
// File created : 2024, 06, 16 00:06
#endregion

using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Vvr.Provider;
using Vvr.Session.AssetManagement;
using Vvr.Session.ContentView.Core;

namespace Vvr.Session.ContentView.Modal
{
    [UsedImplicitly]
    public sealed class ModalViewSession : ContentViewChildSession<ModalViewEvent, IModalViewProvider>
    {
        private IAssetProvider m_AssetProvider;

        public override string DisplayName => nameof(ModalViewSession);

        protected override async UniTask OnInitialize(IParentSession session, ContentViewSessionData data)
        {
            await base.OnInitialize(session, data);

            m_AssetProvider = await CreateSessionOnBackground<AssetSession>(default);

            TypedEventHandler
                .Register<ModalViewCloseContext>(ModalViewEvent.Close, OnViewClose)
                .Register(ModalViewEvent.Open, OnViewOpen)
                ;
        }
        private async UniTask OnViewClose(ModalViewEvent e, ModalViewCloseContext ctx)
        {
            if (!ViewProvider.TryGetModal(ctx.ModalType, out var ins))
                return;

            this.Detach(ins);

            var task = ViewProvider.CloseAsync(ctx, ReserveToken)
                .AttachExternalCancellation(ReserveToken)
                .SuppressCancellationThrow()
                ;

            if (ctx.WaitForCompletion)
                await task;
            else
                task.Forget();
        }

        private async UniTask OnViewOpen(ModalViewEvent e, object ctx)
        {
            if (ctx is not IModalViewContext context)
            {
                throw new InvalidOperationException();
            }

            var ins = await ViewProvider.OpenAsync(
                CanvasViewProvider, m_AssetProvider, ctx, ReserveToken);
            this.Inject(ins);

            if (context.WaitForCompletion)
                await ViewProvider.WaitForCloseAsync(context.ModalType)
                    .AttachExternalCancellation(ReserveToken)
                    .SuppressCancellationThrow()
                    ;
        }
    }
}