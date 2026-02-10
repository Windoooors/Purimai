using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UI.Result;
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

        private static List<ChartRankData> _dataList = Directory.Exists(Application.persistentDataPath)? (
            !File.Exists(SavePath)
                ? new List<ChartRankData>()
                : JsonConvert.DeserializeObject<List<ChartRankData>>(
                    File.ReadAllText(SavePath))) : new List<ChartRankData>();

        private static HashSet<ChartRankData> _dataHashSet;

        public static string GetRankName(float achievement, int score, int totalScoreWithExtraScore,
            AchievementType achievementType)
        {
            switch (achievementType)
            {
                case AchievementType.Finale:
                    if (score == totalScoreWithExtraScore)
                        return "SSS+";

                    return achievement switch
                    {
                        >= 100 => "SSS",
                        < 100 and >= 99.5f => "SS+",
                        < 99.5f and >= 99 => "SS",
                        < 99 and >= 98 => "S+",
                        < 98 and >= 97 => "S",
                        < 97 and >= 94 => "AAA",
                        < 94 and >= 90 => "AA",
                        < 90 and >= 80 => "A",
                        < 80 and >= 60 => "B",
                        < 60 and >= 40 => "C",
                        < 40 and >= 20 => "D",
                        < 20 and >= 10 => "E",
                        < 10 => "F",
                        _ => "Unknown"
                    };
                case AchievementType.Dx:
                    return achievement switch
                    {
                        >= 100.5f => "SSS+",
                        < 100.5f and >= 100 => "SSS",
                        < 100 and >= 99.5f => "SS+",
                        < 99.5f and >= 99 => "SS",
                        < 99 and >= 98 => "S+",
                        < 98 and >= 97 => "S",
                        < 97 and >= 94 => "AAA",
                        < 94 and >= 90 => "AA",
                        < 90 and >= 80 => "A",
                        < 80 and >= 75 => "BBB",
                        < 75 and >= 70 => "BB",
                        < 70 and >= 60 => "B",
                        < 60 and >= 50 => "C",
                        < 50 => "D",
                        _ => "Unknown"
                    };
            }

            return "Unknown";
        }

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