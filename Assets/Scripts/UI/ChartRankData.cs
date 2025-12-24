using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace UI
{
    public class LevelAchievement
    {
        public ScorePair DxBestAchievement { get; set; } = new();
        public ScorePair FinaleBestAchievement { get; set; } = new();

        public class ScorePair
        {
            public float DxAchievement;
            public float FinaleAchievement;
            public int Score;
        }
    }

    public enum FcState
    {
        None,
        Fc,
        FcGold,
        Ap
    }

    public class LevelRankData
    {
        [JsonProperty] public readonly int DifficultyIndex;

        [JsonProperty] public int Combo;

        [JsonProperty] public FcState FcState = FcState.None;

        [JsonProperty] public LevelAchievement LevelAchievements = new();

        [JsonProperty] public int TotalScore;

        public LevelRankData(int difficultyIndex)
        {
            DifficultyIndex = difficultyIndex;
        }

        public LevelRankData()
        {
            DifficultyIndex = -1;
        }

        public override int GetHashCode()
        {
            return DifficultyIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is not LevelRankData other)
                return false;
            return DifficultyIndex == other.DifficultyIndex;
        }
    }

    public static class ChartRankDataManager
    {
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "player_scores.json");

        private static List<ChartRankData> _dataList = !File.Exists(SavePath)
            ? new List<ChartRankData>()
            : JsonConvert.DeserializeObject<List<ChartRankData>>(
                File.ReadAllText(SavePath));

        private static HashSet<ChartRankData> _dataHashSet;

        public static ChartRankData GetChartRankData(string maidataPath)
        {
            _dataHashSet ??= _dataList.ToHashSet();

            return _dataHashSet?.ToList().Find(x => x.MaidataPath == maidataPath);
        }

        public static ChartRankData AddChartRankData(string maidataPath)
        {
            _dataHashSet ??= _dataList.ToHashSet();

            var data = new ChartRankData(maidataPath);

            _dataHashSet.Add(data);

            _dataList = _dataHashSet.ToList();

            return data;
        }

        public static void Save()
        {
            File.WriteAllText(SavePath, JsonConvert.SerializeObject(_dataList));
        }
    }

    public class ChartRankData
    {
        [JsonProperty] public readonly string MaidataPath;

        private HashSet<LevelRankData> _levelRankDataHashSet;

        [JsonProperty("LevelRankData")] private List<LevelRankData> _levelRankDataList = new();

        public ChartRankData(string maidataPath)
        {
            MaidataPath = maidataPath;
        }

        public LevelRankData GetLevelRankData(int difficultyIndex)
        {
            _levelRankDataHashSet ??= _levelRankDataList.ToHashSet();

            return _levelRankDataHashSet?.ToList().Find(x => x.DifficultyIndex == difficultyIndex);
        }

        public LevelRankData AddLevelRankData(int difficultyIndex)
        {
            var data = new LevelRankData(difficultyIndex);

            _levelRankDataHashSet ??= _levelRankDataList.ToHashSet();

            _levelRankDataHashSet.Add(data);

            _levelRankDataList = _levelRankDataHashSet.ToList();

            return data;
        }

        public override int GetHashCode()
        {
            return MaidataPath?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is not ChartRankData other)
                return false;

            return MaidataPath == other.MaidataPath;
        }
    }
}