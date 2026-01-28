using System;
using UI.GameSettings;
using UI.Result;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class ScoreContentPanel : VisualElement
    {
        [UxmlAttribute("default-level-color")]
        public Color DefaultColor = new Color(238 / 255f, 215 / 255f, 250 / 255f);
        [UxmlAttribute("level-colors")]
        public Color[] Colors =
        {
            new Color(169/255f, 220/255f,1),
            new Color(154/255f, 215/255f,100/255f),
            new Color(239/255f, 233/255f,110/255f),
            new Color(166/255f, 48/255f,48/255f),
            new Color(114/255f, 60/255f,144/255f),
            new Color(238/255f, 215/255f,250/255f)
        };
        
        [UxmlAttribute("default-text-color")]
        public Color DefaultTextColor = Color.black;
        [UxmlAttribute("text-colors")]
        public Color[] TextColors =
        {
            Color.black, 
            Color.black,
            Color.black,
            Color.white, 
            Color.white, 
            Color.black
        };
        
        public ScoreContentPanel()
        {
            var asset = UIManager.GetInstance().scoreContentVisualTreeAsset;
            asset.CloneTree(this);
            
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }
        
        private VisualElement _scoreContentPanel;
        
        private Label _achievementTitleLabel;
        private Label _achievementLabel;
        private Label _subAchievementLabel;
        private Label _rankTitleLabel;
        private Label _rankLabel;
        
        private Label _difficultyRateLabel;
        private Label _charterNameLabel;
        private Label _difficultyNameLabel;

        private readonly string _defaultDifficultyName = "Unknown";
        private readonly string[] _difficultyNames = 
        {
            "Ez",
            "Bas",
            "Avd",
            "Exp",
            "Mas",
            "Re",
            "UTAGE"
        };

        private VisualElement _background;
        
        private void OnAttach(AttachToPanelEvent evt)
        {
            style.unityBackgroundImageTintColor = DefaultColor;
            
            AddToClassList("score-content-panel");
            
            _scoreContentPanel = this.Q<VisualElement>("score-information-panel");
            
            _background = this.Q<VisualElement>("score-content-background");
            
            _achievementTitleLabel = _background.Q<Label>("achievement-title-label");
            _achievementLabel = _background.Q<Label>("achievement-label");
            _subAchievementLabel = _background.Q<Label>("sub-achievement-label");
            _rankTitleLabel = _background.Q<Label>("rank-title-label");
            _rankLabel = _background.Q<Label>("rank-label");
            _difficultyRateLabel = _background.Q<Label>("difficulty-rate-label");
            _charterNameLabel = _background.Q<Label>("charter-name-label");
            _difficultyNameLabel = _background.Q<Label>("difficulty-name-label");
            var button = _background.Q<Button>();

            button.clicked += () =>
            {
                OnClicked?.Invoke(this, EventArgs.Empty);
            };
        }

        public EventHandler OnClicked;
        
        public void SetScoreData(ChartRankData data, int difficultyIndex)
        {
            if (data?.GetLevelRankData(difficultyIndex) is null)
            {
                _scoreContentPanel.style.display = DisplayStyle.None;
                _scoreContentPanel.style.opacity = 0;
                return;
            }
            
            var levelRankData = data.GetLevelRankData(difficultyIndex);

            var levelAchievements = levelRankData.LevelAchievements;

            var useDxRatingSystem = SettingsPool.GetValue("game.achievement_type") == 1;
            var useScore =  SettingsPool.GetValue("game.score_indicator_type") == 0;
            
            _scoreContentPanel.style.display = DisplayStyle.Flex;
            _scoreContentPanel.style.opacity = 1;

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

            _rankLabel.text = ChartRankDataManager.GetRankName(achievement, score, levelRankData.TotalScore,
                useDxRatingSystem ? AchievementType.Dx : AchievementType.Finale);
        }
        
        public void SetChartInformationData(Maidata maidata, int difficultyIndex)
        {
            _difficultyRateLabel.text = maidata.Difficulties[difficultyIndex];

            _charterNameLabel.text = maidata.Designers[difficultyIndex] == ""
                ? maidata.MainChartDesigner
                : maidata.Designers[difficultyIndex];

            if (maidata.IsUtage)
                difficultyIndex = 6;
            
            _difficultyNameLabel.text = difficultyIndex >= _difficultyNames.Length ? _defaultDifficultyName : _difficultyNames[difficultyIndex];
            _background.style.unityBackgroundImageTintColor =
                difficultyIndex >= Colors.Length ? DefaultColor : Colors[difficultyIndex];
            _achievementTitleLabel.style.color =
                difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _difficultyNameLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _achievementLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _subAchievementLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _rankTitleLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _rankLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _difficultyRateLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            _charterNameLabel.style.color = difficultyIndex >= TextColors.Length ? DefaultTextColor : TextColors[difficultyIndex];
            

        }
    }
}