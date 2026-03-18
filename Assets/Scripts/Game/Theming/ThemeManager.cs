using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Logger = Logging.Logger;

namespace Game.Theming
{
    public static class ThemeManager
    {
        public static readonly List<ThemeData> SkinDataList = new();
        public static ThemeData DefaultTheme { get; private set; }
        public static bool HoldColorRelatedHoldEffect { get; set; }

        public static void Load()
        {
            var path = Path.Combine(Application.persistentDataPath, "Skins");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var skinPaths = Directory.GetDirectories(path);

            SkinDataList.Clear();

            DefaultTheme = new ThemeData
            {
                Path = Path.Combine("default_skin"),
                AppliedModules = 0,
                themeDataDto = new ThemeDataDto
                {
                    DescriptionEn = "Purimai default theme",
                    DescriptionZh = "Purimai 的默认主题",
                    Author = "-",
                    DisplayNameEn = "Default",
                    DisplayNameZh = "默认",
                    LoaderVersion = "1.0.0",
                    Version = "1.0.0",
                    Data = Array.Empty<SkinPieceDataDto>(),
                    SfxData = Array.Empty<SfxPieceDataDto>()
                },
                InStreamingAssets = true
            };

            SkinDataList.Add(DefaultTheme);

            foreach (var skinPath in skinPaths)
            {
                var metaPath = Path.Combine(skinPath, "metadata.json");
                if (!File.Exists(metaPath))
                    continue;

                try
                {
                    Logger.LogInfo($"Found skin path {skinPath}.");

                    var content = JsonConvert.DeserializeObject<ThemeDataDto>(File.ReadAllText(metaPath));

                    var skinData = new ThemeData
                    {
                        Path = skinPath,
                        themeDataDto = content
                    };

                    SkinDataList.Add(skinData);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to load SkinData: " + e.Message + "\nStack Trace:" + e.StackTrace);
                }
            }

            var savePath = Path.Combine(Application.persistentDataPath, "skin_settings.json");

            var skinSettings = File.Exists(savePath)
                ? JsonConvert.DeserializeObject<SkinSettingsItem[]>(
                    File.ReadAllText(savePath))
                : Array.Empty<SkinSettingsItem>();

            foreach (var skinSettingsItem in skinSettings)
            {
                var match = SkinDataList.Find(x => x.Path == skinSettingsItem.Path);
                if (match == null)
                    continue;

                match.AppliedModules = skinSettingsItem.AppliedModules;
            }

            if (SkinDataList.Count == 1 || skinSettings.Length == 0) SkinDataList[0].AppliedModules = 0b111111;
        }
    }
}