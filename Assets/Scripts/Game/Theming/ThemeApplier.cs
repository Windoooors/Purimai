using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
    }
}