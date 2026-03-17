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

    [JsonObject]
    public class SkinPieceDataDto
    {
        public string Key;
        public string Path;
    }

    [JsonObject]
    public class SkinDataDto
    {
        public string Author;

        public SkinPieceDataDto[] Data;
        public string DescriptionEn;
        public string DescriptionZh;
        public string DisplayNameEn;
        public string DisplayNameZh;

        public string LoaderVersion;

        public string Version;
    }

    public class SkinData
    {
        [JsonProperty] public int AppliedModules;

        [NonSerialized] public bool InStreamingAssets;

        [JsonProperty] public string Path;

        [NonSerialized] public SkinDataDto SkinDataDto;
    }
}