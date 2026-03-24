using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using System;
using EditorScript;
#endif
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Game.Theming
{
    [JsonObject]
    public class SkinSettingsItem
    {
        public int AppliedModules;
        public string Path;
    }

    public class ThemeApplier : MonoBehaviour
    {
        public const int ModuleCount = 6;

        private static ThemeApplier _instance;
        public List<SkinPieceData> tapSkinDataList = new();
        public List<SkinPieceData> holdSkinDataList = new();
        public List<SkinPieceData> starSkinDataList = new();
        public List<SkinPieceData> slideSkinDataList = new();
        public List<SkinPieceData> judgeDisplaySkinDataList = new();
        public List<SkinPieceData> miscSkinDataList = new();

        public static ThemeApplier Instance => _instance ??= FindAnyObjectByType<ThemeApplier>();

        private void Awake()
        {
            ThemeManager.Load();

            LoadTheme();
        }

        public void LoadTheme()
        {
            SkinApplier.LoadSkin();
            SfxApplier.LoadSfx();

            var savePath = Path.Combine(Application.persistentDataPath, "skin_settings.json");

            File.WriteAllText(savePath, JsonConvert.SerializeObject
            (ThemeManager.SkinDataList.Select(x =>
                new SkinSettingsItem
                {
                    Path = x.Path,
                    AppliedModules = x.AppliedModules
                }).ToArray()));
        }

        public List<SfxPieceData> GetSfxPieceDataList()
        {
            return SfxManager.Instance.GameSoundNameData.audioSoundNameDataDict.Keys.Select(x => new SfxPieceData
            {
                key = x
            }).ToList();
        }

        public List<SkinPieceData> GetSkinPieceDataList(int index)
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

#if UNITY_EDITOR
        [InspectorButton]
        private void UpdateDefaultSkin()
        {
            var defaultSkinPath = Application.streamingAssetsPath + "/default_skin";

            foreach (var file in Directory.GetFiles(defaultSkinPath)) File.Delete(file);

            foreach (var skinData in tapSkinDataList)
                File.Copy(AssetDatabase.GetAssetPath(skinData.sprite), defaultSkinPath + "/" + skinData.key + ".png",
                    true);
            foreach (var skinData in holdSkinDataList)
                File.Copy(AssetDatabase.GetAssetPath(skinData.sprite), defaultSkinPath + "/" + skinData.key + ".png",
                    true);
            foreach (var skinData in starSkinDataList)
                File.Copy(AssetDatabase.GetAssetPath(skinData.sprite), defaultSkinPath + "/" + skinData.key + ".png",
                    true);
            foreach (var skinData in slideSkinDataList)
                File.Copy(AssetDatabase.GetAssetPath(skinData.sprite), defaultSkinPath + "/" + skinData.key + ".png",
                    true);
            foreach (var skinData in miscSkinDataList)
                File.Copy(AssetDatabase.GetAssetPath(skinData.sprite), defaultSkinPath + "/" + skinData.key + ".png",
                    true);
            foreach (var skinData in judgeDisplaySkinDataList)
                File.Copy(AssetDatabase.GetAssetPath(skinData.sprite), defaultSkinPath + "/" + skinData.key + ".png",
                    true);
        }
        
        [InspectorButton]
        private void GenerateDefaultMetadata()
        {
            var allList = new List<SkinPieceData>();
            allList.AddRange(tapSkinDataList);
            allList.AddRange(holdSkinDataList);
            allList.AddRange(starSkinDataList);
            allList.AddRange(slideSkinDataList);
            allList.AddRange(judgeDisplaySkinDataList);
            allList.AddRange(miscSkinDataList);

            var dataDtoEnum = allList.Select(x => new SkinPieceDataDto()
            {
                Key = x.key,
                Path = ""
            });

            var content = JsonConvert.SerializeObject(new ThemeDataDto()
            {
                Author = "Unknown",
                Data = dataDtoEnum.ToArray(),
                SfxData = Array.Empty<SfxPieceDataDto>(),
                DescriptionEn = "-",
                DescriptionZh = "-",
                DisplayNameEn = "Unknown",
                DisplayNameZh = "Unknown",
                HasJudgeCircleColor = true,
                HoldColorRelatedHoldEffect = false
            });

            Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "Export"));
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Export/Metadata.json"), content);
        }
#endif
    }
}