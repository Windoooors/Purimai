using System;
using System.Linq;
using UI.LevelSelection;
using UI.Result;
using UI.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class ScoreContentPanel : VisualElement
    {
        private readonly string[] _difficultyNames =
        {
            "UNKNOWN",
            "EZ",
            "BAS",
            "AVD",
            "EXP",
            "MAS",
            "RE",
            "UTAGE"
        };

        private Label _achievementLabel;
        private Label _fcLabel;

        private Label _achievementTitleLabel;
        private VisualElement _chartBackground;
        private VisualElement _chartContentPanel;
        private Label _charterNameLabel;
        private Maidata.Chart _currentChart;

        private Maidata _currentMaidata;
        private Label _difficultyNameLabel;

        private Label _difficultyRateLabel;

        private LevelSelectionManager.SortingRules _groupByRule;
        private VisualElement _noScoreContentPanel;
        private Label _rankLabel;
        private Label _rankTitleLabel;
        
        [UxmlAttribute("allow-difficulty-change")]
        public bool AllowDifficultyChange = true;

        private VisualElement _scoreBackground;

        private VisualElement _scoreContentPanel;
        private Label _subAchievementLabel;

        public int AlphabeticallySelectedDifficultyIndex;

        [UxmlAttribute("level-colors")] public Color[] Colors =
        {
            new(238 / 255f, 215 / 255f, 250 / 255f),
            new(169 / 255f, 220 / 255f, 1),
            new(154 / 255f, 215 / 255f, 100 / 255f),
            new(239 / 255f, 233 / 255f, 110 / 255f),
            new(166 / 255f, 48 / 255f, 48 / 255f),
            new(114 / 255f, 60 / 255f, 144 / 255f),
            new(238 / 255f, 215 / 255f, 250 / 255f)
        };

        public EventHandler<DifficultyChangeEventArgs> OnDifficultyTendsToChange;

        public EventHandler OnScoringMethodChanged;

        [UxmlAttribute("text-colors")] public Color[] TextColors =
        {
            Color.black,
            Color.black,
            Color.black,
            Color.black,
            Color.white,
            Color.white,
            Color.black
        };

        [UxmlAttribute("text-gradient-names")] public string[] TextGradients =
        {
            "difficulty-name-watermark-dark-to-white",
            "difficulty-name-watermark-dark-to-white",
            "difficulty-name-watermark-dark-to-white",
            "difficulty-name-watermark-dark-to-white",
            "difficulty-name-watermark-white-to-dark",
            "difficulty-name-watermark-white-to-dark",
            "difficulty-name-watermark-dark-to-white"
        };

        public ScoreContentPanel()
        {
            var asset = Resources.Load<VisualTreeAsset>("UI/UXML/ScoreContent");
            asset.CloneTree(this);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(e => { SettingsManager.OnSettingsChanged -= SetScoreData; });

            AlphabeticallySelectedDifficultyIndex = PlayerPrefs.GetInt("SelectedDifficultyIndex");
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            style.unityBackgroundImageTintColor = Colors[0];

            AddToClassList("score-content-panel");

            _scoreContentPanel = this.Q<VisualElement>("score-information-panel");
            _noScoreContentPanel = this.Q<VisualElement>("score-no-information-notice-panel");

            _scoreBackground = this.Q<VisualElement>("score-content-background");
            _chartBackground = this.Q<VisualElement>("chart-information-background");

            _achievementTitleLabel = _scoreBackground.Q<Label>("achievement-title-label");
            _achievementLabel = _scoreBackground.Q<Label>("achievement-label");
            _subAchievementLabel = _scoreBackground.Q<Label>("sub-achievement-label");
            _rankTitleLabel = _scoreBackground.Q<Label>("rank-title-label");
            _rankLabel = _scoreBackground.Q<Label>("rank-label");
            _fcLabel = _scoreBackground.Q<Label>("fc-label");

            _difficultyRateLabel = _chartBackground.Q<Label>("difficulty-rate-label");
            _charterNameLabel = _chartBackground.Q<Label>("charter-name-label");
            _difficultyNameLabel = _chartBackground.Q<Label>("difficulty-name-label");

            var scoreButton = _scoreBackground.Q<Button>();
            scoreButton.clicked += () => { OnScoringMethodChanged?.Invoke(this, EventArgs.Empty); };

            var chartButton = _chartBackground.Q<Button>();
            chartButton.clicked += ChangeDifficulty;

            _groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => LevelSelectionManager.SortingRules.Alphabet,
                _ => LevelSelectionManager.SortingRules.Difficulty
            };

            SettingsManager.OnSettingsChanged += SetScoreData;
        }

        private void SetScoreData()
        {
            SetScoreData(ChartRankDataManager.GetChartRankData(_currentMaidata.MaidataDirectoryName),
                _currentChart.DifficultyIndex);
        }

        public void SetScoreData(ChartRankData data, int difficultyIndex)
        {
            _groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => LevelSelectionManager.SortingRules.Alphabet,
                _ => LevelSelectionManager.SortingRules.Difficulty
            };

            if (data?.GetLevelRankData(difficultyIndex) is null)
            {
                _scoreContentPanel.style.display = DisplayStyle.None;
                _scoreContentPanel.style.opacity = 0;

                _noScoreContentPanel.style.display = DisplayStyle.Flex;
                _noScoreContentPanel.style.opacity = 1;

                return;
            }

            var levelRankData = data.GetLevelRankData(difficultyIndex);

            var levelAchievements = levelRankData.LevelAchievements;

            var useDxRatingSystem = SettingsPool.GetValue("scoring_methods.achievement_type") == 0;
            var useScore = SettingsPool.GetValue("scoring_methods.score_indicator_type") == 0;

            _scoreContentPanel.style.display = DisplayStyle.Flex;
            _scoreContentPanel.style.opacity = 1;

            _noScoreContentPanel.style.display = DisplayStyle.None;
            _noScoreContentPanel.style.opacity = 0;

            if (useScore)
            {
                _achievementTitleLabel.text = "Score";
                _achievementLabel.text = useDxRatingSystem
                    ? levelAchievements.DxBestAchievement.Score.ToString()
                    : levelAchievements.FinaleBestAchievement.Score.ToString();
            }
            else
            {
                _achievementTitleLabel.text = "Achievement";
                _achievementLabel.text = useDxRatingSystem
                    ? levelAchievements.DxBestAchievement.DxAchievement.ToString("0.0000") + "%"
                    : levelAchievements.FinaleBestAchievement.FinaleAchievement.ToString("0.00") + "%";
            }

            _subAchievementLabel.text = useDxRatingSystem
                ? "F.A. " + levelAchievements.DxBestAchievement.FinaleAchievement.ToString("0.00") + "%"
                : "D.A " + levelAchievements.FinaleBestAchievement.DxAchievement.ToString("0.0000") + "%";

            var achievement = useDxRatingSystem
                ? levelAchievements.DxBestAchievement.DxAchievement
                : levelAchievements.FinaleBestAchievement.FinaleAchievement;

            var score = useDxRatingSystem
                ? levelAchievements.DxBestAchievement.Score
                : levelAchievements.FinaleBestAchievement.Score;

            var fcText = levelRankData.FcState switch
            {
                FcState.None => "Played",
                FcState.Fc => "FC",
                FcState.FcGold => "<color=white><gradient=\"ap-display\">FC</gradient></color>",
                FcState.Ap => "<color=white><gradient=\"ap-display\">AP</gradient></color>",
                _ => "Unknown"
            };

            _fcLabel.text = fcText;
            
            _rankLabel.text = ChartRankDataManager.GetRankName(achievement, score, levelRankData.TotalScore,
                useDxRatingSystem ? AchievementType.Dx : AchievementType.Finale);
        }

        public void SetChartInformationData(Maidata maidata, int difficultyIndex)
        {
            _groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => LevelSelectionManager.SortingRules.Alphabet,
                _ => LevelSelectionManager.SortingRules.Difficulty
            };

            _currentMaidata = maidata;

            var chart = maidata.Charts.ToList().Find(x => x.DifficultyIndex == difficultyIndex);

            if (chart == null)
            {
                foreach (var maidataChart in maidata.Charts)
                    if (maidataChart.DifficultyIndex >= difficultyIndex)
                        chart = maidataChart;

                if (chart == _currentChart || chart == null) chart = maidata.Charts[0];

                difficultyIndex = chart.DifficultyIndex;
            }

            if (_groupByRule == LevelSelectionManager.SortingRules.Alphabet)
            {
                AlphabeticallySelectedDifficultyIndex = chart.DifficultyIndex;
                PlayerPrefs.SetInt("SelectedDifficultyIndex", AlphabeticallySelectedDifficultyIndex);
            }

            _currentChart = chart;

            _difficultyRateLabel.text = chart.DifficultyString;

            _charterNameLabel.text = chart.Designer;

            if (maidata.IsUtage)
                difficultyIndex = 7;

            _difficultyNameLabel.text =
                $"<color=white><gradient=\"{(difficultyIndex >= TextGradients.Length ? TextGradients[0] : TextGradients[difficultyIndex])}\">{(difficultyIndex >= _difficultyNames.Length ? _difficultyNames[0] : _difficultyNames[difficultyIndex])}</gradient></color>";
            _chartBackground.style.unityBackgroundImageTintColor =
                difficultyIndex >= Colors.Length ? Colors[0] : Colors[difficultyIndex];
            //_difficultyNameLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _difficultyRateLabel.style.color =
                difficultyIndex >= TextColors.Length ? TextColors[0] : TextColors[difficultyIndex];
            _charterNameLabel.style.color =
                difficultyIndex >= TextColors.Length ? TextColors[0] : TextColors[difficultyIndex];
        }

        private void ChangeDifficulty()
        {
            if (!AllowDifficultyChange)
                return;
            
            var targetChartIndex = 0;
            var currentChartIndex = 0;
            
            var originalAlphabeticallySelectedDifficultyIndex = AlphabeticallySelectedDifficultyIndex;

            switch (_groupByRule)
            {
                case LevelSelectionManager.SortingRules.Alphabet:
                    AlphabeticallySelectedDifficultyIndex++;
                    SetChartInformationData(_currentMaidata, AlphabeticallySelectedDifficultyIndex);
                    if (originalAlphabeticallySelectedDifficultyIndex != AlphabeticallySelectedDifficultyIndex)
                        SetScoreData(ChartRankDataManager.GetChartRankData(_currentMaidata.MaidataDirectoryName),
                            AlphabeticallySelectedDifficultyIndex);
                    targetChartIndex = AlphabeticallySelectedDifficultyIndex;
                    break;
                case LevelSelectionManager.SortingRules.Difficulty:
                    currentChartIndex = _currentMaidata.Charts.ToList().IndexOf(_currentChart);
                    targetChartIndex = currentChartIndex + 1;

                    if (targetChartIndex >= _currentMaidata.Charts.Length)
                        targetChartIndex = 0;

                    if (targetChartIndex < 0) targetChartIndex = _currentMaidata.Charts.Length - 1;

                    break;
            }

            if (_groupByRule == LevelSelectionManager.SortingRules.Difficulty)
                OnDifficultyTendsToChange?.Invoke(this, new DifficultyChangeEventArgs
                {
                    TargetChartIndex = targetChartIndex,
                    Direction = currentChartIndex > targetChartIndex ? -1 : 1
                });
        }

        public class DifficultyChangeEventArgs : EventArgs
        {
            public int Direction;
            public int TargetChartIndex;
        }
    }
}