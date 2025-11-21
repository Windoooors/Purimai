using Game;
using LitMotion;
using TMPro;
using UI.LevelSelection;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Result
{
    public class ResultController : UIScriptWithAnimation
    {
        private static ResultController _instance;
        public Image backgroundImage;
        public Image blurredSongCoverImage;
        public Image[] songCoverImage;

        public CanvasGroup resultDifficultyIndicatorCanvasGroup;

        public TextMeshProUGUI artistText;
        public TextMeshProUGUI songTitleText;
        public TextMeshProUGUI achievementTitleText;
        public TextMeshProUGUI achievementText;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI rankTitleText;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI difficultyNameText;
        public TextMeshProUGUI charterNameText;
        public Image difficultyIndicatorBackgroundImage;

        public CanvasGroup canvasGroup;

        private void Awake()
        {
            _instance = this;
            canvasGroup.gameObject.SetActive(false);
        }

        public void Initialize(LevelListController.Maidata maidata, int difficultyIndex)
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0;

            backgroundImage.sprite = maidata.SongCoverBlurredAsBackground;
            blurredSongCoverImage.sprite = maidata.SongCoverBlurred;

            foreach (var image in songCoverImage) image.sprite = maidata.SongCover;

            artistText.text = maidata.Artist;
            songTitleText.text = maidata.Title;

            difficultyText.text = maidata.Difficulties[difficultyIndex];

            var designerName = maidata.Designers[difficultyIndex];

            designerName = designerName == "\r" ? maidata.MainChartDesigner : designerName;
            designerName = designerName == "\r" ? "Unknown Designer" : designerName;

            charterNameText.text = designerName;

            difficultyNameText.text = (maidata.IsUtage ? 6 : difficultyIndex) switch
            {
                0 => "EZ",
                1 => "BAS",
                2 => "ADV",
                3 => "EXP",
                4 => "MAS",
                5 => "RE",
                6 => "UTAGE",
                _ => ""
            };

            var textColor = DifficultyIndicator.Instance.textColors[maidata.IsUtage ? 5 : difficultyIndex];

            difficultyText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyText.color.a);
            charterNameText.color = new Color(textColor.r, textColor.g, textColor.b, charterNameText.color.a);
            difficultyNameText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyNameText.color.a);

            achievementTitleText.color = new Color(textColor.r, textColor.g, textColor.b, achievementTitleText.color.a);
            achievementText.color = new Color(textColor.r, textColor.g, textColor.b, achievementText.color.a);
            rankText.color = new Color(textColor.r, textColor.g, textColor.b, rankText.color.a);
            rankTitleText.color = new Color(textColor.r, textColor.g, textColor.b, rankTitleText.color.a);

            difficultyNameText.colorGradient =
                DifficultyIndicator.Instance.textGradientColors[maidata.IsUtage ? 5 : difficultyIndex];
            difficultyIndicatorBackgroundImage.color =
                DifficultyIndicator.Instance.backgroundColors[maidata.IsUtage ? 5 : difficultyIndex];

            canvasGroup.gameObject.SetActive(false);
        }

        public void ShowResult()
        {
            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 1;

            achievementText.text = Scoreboard.GetAchievement().ToString("0.00") + "%";
            rankText.text = GetRankName(Scoreboard.GetAchievement(), Scoreboard.Score,
                Scoreboard.TotalScoreWithExtraScore);

            var originalPosition = resultDifficultyIndicatorCanvasGroup.transform.position;

            resultDifficultyIndicatorCanvasGroup.transform.position = originalPosition + new Vector3(2, 0, 0);

            AddMotionHandle(LMotion.Create(0, 1f, 1f).WithEase(Ease.OutExpo).Bind(x =>
            {
                UIManager.Instance.maskCanvasGroup.alpha = x;
                resultDifficultyIndicatorCanvasGroup.transform.position =
                    originalPosition + new Vector3(2 - x * 2, 0, 0);
            }));
        }

        public static ResultController GetInstance()
        {
            return !_instance ? FindAnyObjectByType<ResultController>() : _instance;
        }

        private static string GetRankName(float achievement, int score, int totalScoreWithExtraScore)
        {
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
        }
    }
}