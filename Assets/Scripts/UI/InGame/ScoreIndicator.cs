using TMPro;
using UI.Settings;
using UI.Result;
using UnityEngine;

namespace UI.InGame
{
    public class ScoreIndicator : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI titleText;

        private int _settingsIndex;

        private AchievementType _achievementType;
            
        private void Start()
        {
            _achievementType = (AchievementType)SettingsPool.GetValue("scoring_methods.achievement_type");
        }

        private void Update()
        {
            titleText.text = _settingsIndex switch
            {
                0 => "",
                1 => "COMBO",
                2 => "ACHIEVEMENT",
                3 => "ACHIEVEMENT",
                4 => "SCORE",
                5 => "SCORE",
                _ => ""
            };
            scoreText.text = _settingsIndex switch
            {
                0 => "",
                1 => Scoreboard.Combo.ToString(),
                2 => Scoreboard.GetCurrentAchievement(_achievementType)
                    .ToString(_achievementType == AchievementType.Finale ? "0.00" : "0.0000") + "%",
                3 => (Scoreboard.GetDeltaAchievement(_achievementType) + 100).ToString(
                    _achievementType == AchievementType.Finale ? "0.00" : "0.0000") + "%",
                4 => Scoreboard.GetScore().ToString(),
                5 => (Scoreboard.GetDeltaScore(AchievementType.Finale).deltaBasicScore +
                    Scoreboard.GetTotalScore() - Scoreboard.GetHighestExtraScore()).ToString(),
                _ => ""
            };

            if (_settingsIndex == 1 && Scoreboard.Combo < 2)
            {
                titleText.text = "";
                scoreText.text = "";
            }
        }

        private void OnEnable()
        {
            _settingsIndex = SettingsPool.GetValue("scoring_methods.score_indicator_content");
        }
    }
}