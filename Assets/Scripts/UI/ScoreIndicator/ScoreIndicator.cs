using TMPro;
using UI.GameSettings;
using UI.Result;
using UnityEngine;

namespace UI.ScoreIndicator
{
    public class ScoreIndicator : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI titleText;

        public int settingsIndex;

        private ResultController.AchievementType _achievementType;

        private void Start()
        {
            _achievementType = (ResultController.AchievementType)SettingsPool.GetValue("game.achievement_type");
        }

        private void Update()
        {
            titleText.text = settingsIndex switch
            {
                0 => "",
                1 => "COMBO",
                2 => "ACHIEVEMENT",
                3 => "ACHIEVEMENT",
                4 => "SCORE",
                5 => "SCORE",
                _ => ""
            };
            scoreText.text = settingsIndex switch
            {
                0 => "",
                1 => Scoreboard.Combo.ToString(),
                2 => Scoreboard.GetCurrentAchievement(_achievementType)
                    .ToString(_achievementType == ResultController.AchievementType.Finale ? "0.00" : "0.0000") + "%",
                3 => (Scoreboard.GetDeltaAchievement(_achievementType) + 100).ToString(
                    _achievementType == ResultController.AchievementType.Finale ? "0.00" : "0.0000") + "%",
                4 => Scoreboard.GetScore().ToString(),
                5 => (Scoreboard.GetDeltaScore(ResultController.AchievementType.Finale).deltaBasicScore +
                    Scoreboard.GetTotalScore() - Scoreboard.GetHighestExtraScore()).ToString(),
                _ => ""
            };

            if (settingsIndex == 1 && Scoreboard.Combo < 2)
            {
                titleText.text = "";
                scoreText.text = "";
            }
        }

        private void OnEnable()
        {
            settingsIndex = SettingsPool.GetValue("game.score_indicator_content");
        }
    }
}