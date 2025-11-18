using System;
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

        private void OnEnable()
        {
            settingsIndex = SettingsPool.GetValue("game.score_indicator_content");
        }
        
        private void Update()
        {
            titleText.text = settingsIndex switch
            {
                0 => "",
                1 => "COMBO",
                2 => "ACHIEVEMENT",
                3 => "ACHIEVEMENT",
                _ => ""
            };
            scoreText.text = settingsIndex switch
            {
                0 => "",
                1 => Scoreboard.Combo.ToString(),
                2 => Scoreboard.GetAchievement().ToString("0.00") + "%",
                3 => (Scoreboard.GetDeductedAchievement() + 100).ToString("0.00") + "%",
                _ => ""
            };
        }
    }
}