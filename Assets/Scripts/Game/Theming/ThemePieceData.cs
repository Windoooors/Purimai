using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Theming
{
    [Serializable]
    public class SkinPieceData
    {
        public string key;
        public Sprite sprite;
    }
    
    [Serializable]
    public class SfxPieceData
    {
        public string key;
    }

    [JsonObject]
    public class SfxPieceDataDto
    {
        public string Key;
        public string Path;
    }
    
    [JsonObject]
    public class SkinPieceDataDto
    {
        public string Key;
        public string Path;
    }

    [JsonObject]
    public class ThemeDataDto
    {
        public string Author;

        public SkinPieceDataDto[] Data;
        public SfxPieceDataDto[] SfxData;
        public string DescriptionEn;
        public string DescriptionZh;
        public string DisplayNameEn;
        public string DisplayNameZh;

        public string LoaderVersion;

        public string Version;

        public bool HoldColorRelatedHoldEffect;
    }

    public class ThemeData
    {
        [JsonProperty] public int AppliedModules;

        [NonSerialized] public bool InStreamingAssets;

        [JsonProperty] public string Path;

        [NonSerialized] public ThemeDataDto themeDataDto;
    }
}