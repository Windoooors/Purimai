using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Tables;
using Logger = Logging.Logger;

namespace Game.Theming
{
    public static class SkinManager
    {
        public static readonly List<SkinData> SkinDataList = new();

        public static void Load()
        {
            var path = Path.Combine(Application.persistentDataPath, "Skins");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            
            var skinPaths = Directory.GetDirectories(path);
            
            SkinDataList.Clear();
            
            SkinDataList.Add(new SkinData()
            {
                Path = Path.Combine("default_skin"),
                AppliedModules = 0,
                SkinDataDto = new SkinDataDto()
                {
                    DescriptionEn = "Purimai default theme",
                    DescriptionZh = "Purimai 的默认主题",
                    Author = "-",
                    DisplayNameEn = "Default",
                    DisplayNameZh = "默认",
                    LoaderVersion = "1.0.0",
                    Version = "1.0.0",
                    Data = Array.Empty<SkinPieceDataDto>()
                },
                InStreamingAssets = true
            });

            foreach (var skinPath in skinPaths)
            {
                var metaPath = Path.Combine(skinPath, "metadata.json");
                if (!File.Exists(metaPath))
                    continue;

                try
                {
                    Logger.LogInfo($"Found skin path {skinPath}.");

                    var content = JsonConvert.DeserializeObject<SkinDataDto>(File.ReadAllText(metaPath));

                    var skinData = new SkinData()
                    {
                        Path = skinPath,
                        SkinDataDto = content
                    };
                    
                    SkinDataList.Add(skinData);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to load SkinData: " + e.Message + "\nStack Trace:" + e.StackTrace);
                }
            }

            var savePath = Path.Combine(Application.persistentDataPath, "skin_settings.json");

            var skinSettings = File.Exists(savePath)? JsonConvert.DeserializeObject<SkinSettingsItem[]>(
                File.ReadAllText(savePath)) : Array.Empty<SkinSettingsItem>();
            
            foreach (var skinSettingsItem in skinSettings)
            {
                var match= SkinDataList.Find(x => x.Path == skinSettingsItem.Path);
                if (match == null)
                    continue;
                
                match.AppliedModules = skinSettingsItem.AppliedModules;
            }

            if (SkinDataList.Count == 1 || skinSettings.Length == 0)
            {
                SkinDataList[0].AppliedModules = 0b11111;
            }
        }
    }

    [JsonObject]
    public class SkinSettingsItem
    {
        public string Path;
        public int AppliedModules;
    }

    public class SkinApplier : MonoBehaviour
    {
        public List<SkinPieceData> tapSkinDataList = new();
        public List<SkinPieceData> holdSkinDataList = new();
        public List<SkinPieceData> starSkinDataList = new();
        public List<SkinPieceData> slideSkinDataList = new();
        public List<SkinPieceData> judgeDisplaySkinDataList = new();
        public List<SkinPieceData> miscSkinDataList = new();

        private void Awake()
        {
            SkinManager.Load();

            LoadSkin();
        }

        private List<SkinPieceData> GetSkinPieceDataList(int index)
        {
            switch (index)
            {
                case 0:
                    return tapSkinDataList;
                case 1:
                    return holdSkinDataList;
                case 2:
                    return starSkinDataList;
                case 3:
                    return slideSkinDataList;
                case 4:
                    var result = new List<SkinPieceData>();
                    result.AddRange(judgeDisplaySkinDataList);
                    result.AddRange(miscSkinDataList);
                    return result;
            }

            return null;
        }

        public const int ModuleCount = 5;

        public static SkinApplier Instance => _instance ??= FindAnyObjectByType<SkinApplier>();
        
        private static SkinApplier _instance;

        public void LoadSkin()
        {
            for (int i = 0; i < ModuleCount; i++)
            {
                var mask = 1 << i;

                SkinManager.SkinDataList.ForEach(x =>
                {
                    if ((x.AppliedModules & mask) == 0)
                        return;
                    
                    var list = GetSkinPieceDataList(i);
                    LoadSingleSkinData(x, list);
                });
            }

            var savePath = Path.Combine(Application.persistentDataPath, "skin_settings.json");

            File.WriteAllText(savePath, JsonConvert.SerializeObject
            (SkinManager.SkinDataList.Select(x =>
                new SkinSettingsItem()
                {
                    Path = x.Path,
                    AppliedModules = x.AppliedModules
                }).ToArray()));

            return;

            void LoadSingleSkinData(SkinData skinData, List<SkinPieceData> skinPieceDataArray)
            {
                foreach (var skinPieceData in skinPieceDataArray)
                {
                    var skinPieceDataDto = skinData.SkinDataDto.Data.ToList().Find(x => x.Key == skinPieceData.key);
                    
                    var streaming = skinData.InStreamingAssets;
                    
                    if (!streaming)
                    {
                        var path = skinPieceDataDto != null ? Path.Combine(skinData.Path, skinPieceDataDto.Path) : "";
                        
                        if (!File.Exists(path)) path = Path.Combine(skinData.Path, skinPieceData.key + ".png");
                        if (!File.Exists(path))
                            path = Path.Combine(skinData.Path, "GameSkins/" + skinPieceData.key + ".png");
                        if (!File.Exists(path)) continue;
                        
                        LoadTexture(path, skinPieceData.sprite);
                    }
                    else
                    {
                        /*if (BetterStreamingAssets.DirectoryExists(skinData.Path + "/"))
                            return;*/
                        
                        var path = skinPieceDataDto != null ? Path.Combine(skinData.Path, skinPieceDataDto.Path) : "";
                        
                        if (path == "" || !BetterStreamingAssets.FileExists(path)) path = Path.Combine(skinData.Path, skinPieceData.key + ".png");
                        if (!BetterStreamingAssets.FileExists(path))
                            path = Path.Combine(skinData.Path, "game_skins/" + skinPieceData.key + ".png");
                        if (!BetterStreamingAssets.FileExists(path)) continue;

                        var data = BetterStreamingAssets.ReadAllBytes(path);
                        
                        LoadTextureFromBytes(data, skinPieceData.sprite);
                    }
                }

                return;
                void LoadTexture(string path, Sprite sprite)
                {
                    using var image = Image.Load<Rgba32>(path);

                    using var decoded = new DecodedImage(image);

                    sprite.texture.Reinitialize(decoded.Width, decoded.Height,
                        TextureFormat.RGBA32, false);
                    sprite.texture.SetPixelData(decoded.PixelData, 0);
                    sprite.texture.Apply();
                }

                void LoadTextureFromBytes(byte[] data, Sprite sprite)
                {
                    using var image = Image.Load<Rgba32>(data);

                    using var decoded = new DecodedImage(image);

                    sprite.texture.Reinitialize(decoded.Width, decoded.Height,
                        TextureFormat.RGBA32, false);
                    sprite.texture.SetPixelData(decoded.PixelData, 0);
                    sprite.texture.Apply();
                }
            }
        }
    }
}