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
// File created : 2024, 05, 21 01:05

#endregion

using System;
using System.IO;
using Cathei.BakingSheet;
using Cathei.BakingSheet.Unity;
using Cysharp.Threading.Tasks;
using Vvr.Model;

namespace Vvr.TestClass
{
    public static class TestUtils
    {
        public static async UniTask DownloadSheet()
        {
            const string sheetId = "1210EHg1DeiuPKkWcynhwGYKuEg2DfxmbKICNM4rGw8c";

            string cred = await File.ReadAllTextAsync("Assets/GoogleSheetServiceId_projectf.json");

            // SheetContainerBase
            var logger = new UnityLogger();
            var conv   = new GoogleSheetConverter(sheetId, cred, TimeZoneInfo.Utc);

            GameDataSheets cont = new GameDataSheets(logger);
            await cont.Bake(conv);

            var exporter = new ScriptableObjectSheetExporter("Assets/AddressableResources/Data");
            await cont.Store(exporter);
        }
    }
}