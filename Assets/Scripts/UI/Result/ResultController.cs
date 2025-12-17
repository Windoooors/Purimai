using System;
using Game;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UI.GameSettings;
using UI.LevelSelection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Result
{
    [Serializable]
    public class ScoreboardTextMeshPros
    {
        public TextMeshProUGUI[] tapTexts;
        public TextMeshProUGUI[] holdTexts;
        public TextMeshProUGUI[] slideTexts;
        public TextMeshProUGUI[] breakTexts;

        public TextMeshProUGUI perfectText;
        public TextMeshProUGUI greatText;
        public TextMeshProUGUI goodText;
        public TextMeshProUGUI missText;
    }

    public class ResultController : UIScriptWithAnimation
    {
        public enum AchievementType
        {
            Finale,
            Dx
        }

        private static ResultController _instance;
        public Image backgroundImage;
        public Image blurredSongCoverImage;
        public Image[] songCoverImage;

        public CanvasGroup resultDifficultyIndicatorCanvasGroup;
        public CanvasGroup resultComboIndicatorCanvasGroup;

        public TextMeshProUGUI artistText;
        public TextMeshProUGUI songTitleText;
        public TextMeshProUGUI achievementTitleText;
        public TextMeshProUGUI achievementText;
        public TextMeshProUGUI alternativeAchievementText;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI rankTitleText;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI difficultyNameText;
        public TextMeshProUGUI charterNameText;

        public TextMeshProUGUI comboText;
        public TextMeshProUGUI comboStateText;

        public Image difficultyIndicatorBackgroundImage;

        [FormerlySerializedAs("ScoreboardTextMeshPros")]
        public ScoreboardTextMeshPros scoreboardTextMeshPros;

        public CanvasGroup detailedScoreboardCanvasGroup;

        [FormerlySerializedAs("canvasGroup")] public CanvasGroup resultLayer;
        public CanvasGroup difficultyIndicatorCanvasGroup;

        public Image songCoverBackground;

        private Vector3 _detailedScoreboardPosition;

        private bool _detailedScoreboardShown;
        private int _difficultyIndex;

        private Maidata _maidata;

        private void Awake()
        {
            _instance = this;
            resultLayer.gameObject.SetActive(false);

            _detailedScoreboardPosition = detailedScoreboardCanvasGroup.transform.position;
        }

        public void Initialize(Maidata maidata, int difficultyIndex)
        {
            _maidata = maidata;
            _difficultyIndex = difficultyIndex;

            resultLayer.gameObject.SetActive(true);
            resultLayer.alpha = 0;

            backgroundImage.sprite = maidata.BlurredSongCoverAsBackgroundDecodedImage.GetSprite();
            blurredSongCoverImage.sprite = maidata.BlurredSongCoverDecodedImage.GetSprite();

            foreach (var image in songCoverImage) image.sprite = maidata.SongCoverDecodedImage.GetSprite();

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

            var textColor = DifficultyIndicator.GetInstance().textColors[maidata.IsUtage ? 5 : difficultyIndex];

            difficultyText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyText.color.a);
            charterNameText.color = new Color(textColor.r, textColor.g, textColor.b, charterNameText.color.a);
            difficultyNameText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyNameText.color.a);

            achievementTitleText.color = new Color(textColor.r, textColor.g, textColor.b, achievementTitleText.color.a);
            achievementText.color = new Color(textColor.r, textColor.g, textColor.b, achievementText.color.a);
            alternativeAchievementText.color =
                new Color(textColor.r, textColor.g, textColor.b, achievementText.color.a);
            rankText.color = new Color(textColor.r, textColor.g, textColor.b, rankText.color.a);
            rankTitleText.color = new Color(textColor.r, textColor.g, textColor.b, rankTitleText.color.a);

            difficultyNameText.colorGradient =
                DifficultyIndicator.GetInstance().textGradientColors[maidata.IsUtage ? 5 : difficultyIndex];
            difficultyIndicatorBackgroundImage.color =
                DifficultyIndicator.GetInstance().backgroundColors[maidata.IsUtage ? 5 : difficultyIndex];

            resultLayer.gameObject.SetActive(false);
        }

        private void InitializeScoreboard()
        {
            scoreboardTextMeshPros.perfectText.text = (Scoreboard.BreakCount.GetPerfectCount() +
                                                       Scoreboard.TapCount.GetPerfectCount()
                                                       + Scoreboard.HoldCount.GetPerfectCount() +
                                                       Scoreboard.SlideCount.GetPerfectCount()).ToString();
            scoreboardTextMeshPros.greatText.text = (Scoreboard.BreakCount.GetGreatCount() +
                                                     Scoreboard.TapCount.GetGreatCount()
                                                     + Scoreboard.HoldCount.GetGreatCount() +
                                                     Scoreboard.SlideCount.GetGreatCount()).ToString();
            scoreboardTextMeshPros.goodText.text = (Scoreboard.BreakCount.GoodCount +
                                                    Scoreboard.TapCount.GoodCount
                                                    + Scoreboard.HoldCount.GoodCount +
                                                    Scoreboard.SlideCount.GoodCount).ToString();
            scoreboardTextMeshPros.missText.text = (Scoreboard.BreakCount.MissCount +
                                                    Scoreboard.TapCount.MissCount
                                                    + Scoreboard.HoldCount.MissCount +
                                                    Scoreboard.SlideCount.MissCount).ToString();

            for (var i = 0; i < 4; i++) InitializeDetailScoreboardItem(i);

            return;

            void InitializeDetailScoreboardItem(int type)
            {
                var textMeshPros = type switch
                {
                    0 => scoreboardTextMeshPros.tapTexts,
                    1 => scoreboardTextMeshPros.holdTexts,
                    2 => scoreboardTextMeshPros.slideTexts,
                    _ => scoreboardTextMeshPros.breakTexts
                };

                for (var i = 0; i < 4; i++)
                    textMeshPros[i].text = (type switch
                    {
                        0 => i switch
                        {
                            0 => Scoreboard.TapCount.GetPerfectCount(),
                            1 => Scoreboard.TapCount.GetGreatCount(),
                            2 => Scoreboard.TapCount.GoodCount,
                            _ => Scoreboard.TapCount.MissCount
                        },
                        1 => i switch
                        {
                            0 => Scoreboard.HoldCount.GetPerfectCount(),
                            1 => Scoreboard.HoldCount.GetGreatCount(),
                            2 => Scoreboard.HoldCount.GoodCount,
                            _ => Scoreboard.HoldCount.MissCount
                        },
                        2 => i switch
                        {
                            0 => Scoreboard.SlideCount.GetPerfectCount(),
                            1 => Scoreboard.SlideCount.GetGreatCount(),
                            2 => Scoreboard.SlideCount.GoodCount,
                            _ => Scoreboard.SlideCount.MissCount
                        },
                        _ => i switch
                        {
                            0 => Scoreboard.BreakCount.GetPerfectCount(),
                            1 => Scoreboard.BreakCount.GetGreatCount(),
                            2 => Scoreboard.BreakCount.GoodCount,
                            _ => Scoreboard.BreakCount.MissCount
                        }
                    }).ToString();
            }
        }

        public void ShowResult()
        {
            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            difficultyIndicatorCanvasGroup.alpha = 1;

            songCoverBackground.enabled = false;

            var sheetButton = Button.GetButton(2);
            var retryButton = Button.GetButton(3);
            var returnButton = Button.GetButton(4);
            var versionButton = Button.GetButton(5);
            var scoreButton = Button.GetButton(6);

            var buttonIconSet = UIManager.GetInstance().buttonIcons;

            sheetButton.ChangeIcon(buttonIconSet.sheet);

            var versionIsFinale = SettingsPool.GetValue("game.achievement_type") == 0;
            var showScore = SettingsPool.GetValue("game.score_indicator_type") == 0;

            versionButton.ChangeIcon(versionIsFinale ? buttonIconSet.finale : buttonIconSet.dx);
            scoreButton.ChangeIcon(showScore ? buttonIconSet.score : buttonIconSet.achievement);

            retryButton.ChangeIcon(buttonIconSet.retry);

            returnButton.ChangeIcon(buttonIconSet.back);

            sheetButton.Show();
            returnButton.Show();
            retryButton.Show();
            versionButton.Show();
            scoreButton.Show();

            resultLayer.gameObject.SetActive(true);
            resultLayer.alpha = 1;

            SetScoreIndicatorContent();

            InitializeScoreboard();

            var originalPosition = resultDifficultyIndicatorCanvasGroup.transform.position;
            resultDifficultyIndicatorCanvasGroup.transform.position = originalPosition + new Vector3(2, 0, 0);

            SettingsController.Instance.settingsUiLayer.gameObject.SetActive(false);
            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(false);

            AddMotionHandle(LMotion.Create(0, 1f, 1f).WithOnComplete(() =>
                {
                    SimulatedSensor.OnTap += (sender, args) =>
                    {
                        switch (args.SensorId)
                        {
                            case "A3": SwitchDetailedScoreboard(); break;
                            case "A4": Retry(); break;
                            case "A5": ReturnToLevelSelector(); break;
                            case "A6": ChangeAchievementVersion(); break;
                            case "A7": ChangeScoreOrAchievement(); break;
                        }
                    };

                    UIManager.GetInstance().mask.showMaskGraphic = true;
                }
            ).WithEase(Ease.OutExpo).Bind(x =>
            {
                UIManager.GetInstance().maskCanvasGroup.alpha = x;
                resultDifficultyIndicatorCanvasGroup.transform.position =
                    originalPosition + new Vector3(2 - x * 2, 0, 0);
            }));
        }

        private void Retry()
        {
            ClearMotion(true);

            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            songCoverBackground.enabled = true;

            songCoverBackground.sprite = _maidata.BlurredSongCoverAsBackgroundDecodedImage.GetSprite();

            songCoverBackground.transform.localScale =
                SettingsPool.GetValue("game.blurred_cover") == 1
                    ? 1.1f * Vector3.one
                    : Vector3.one;

            Button.ClearAllMotion();
            Button.GetButton(3).Press();
            Button.HideAll(false);

            var originalPosition = resultDifficultyIndicatorCanvasGroup.transform.position;

            AddMotionHandle(LSequence.Create()
                .Append(
                    LMotion.Create(0, 10f, 0.5f).WithEase(Ease.InExpo)
                        .Bind(x => resultDifficultyIndicatorCanvasGroup.transform.position =
                            originalPosition + new Vector3(x, 0, 0))
                ).Join(
                    LMotion.Create(1f, 0f, 0.5f).WithEase(Ease.InExpo)
                        .WithOnComplete(() => { DifficultyIndicator.GetInstance().ReloadScene(); }).WithOnComplete(() =>
                        {
                            resultDifficultyIndicatorCanvasGroup.transform.position = originalPosition;
                        })
                        .Bind(x =>
                        {
                            resultLayer.alpha = x;
                            difficultyIndicatorCanvasGroup.alpha = x;
                        })
                ).Run());

            Scoreboard.Reset();
        }

        private void ReturnToLevelSelector()
        {
            ClearMotion(true);

            LevelListController.GetInstance().levelSelectionUiLayer.gameObject.SetActive(true);

            DifficultyIndicator.GetInstance().SetScoreIndicatorContent(_maidata.MaidataDirectoryName, _difficultyIndex);

            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            var originalPosition =
                resultDifficultyIndicatorCanvasGroup.transform.position;

            AddMotionHandle(
                LSequence.Create()
                    .Append(
                        LMotion.Create(originalPosition,
                                DifficultyIndicator.GetInstance().difficultyIndicatorCanvasGroup.transform.position,
                                1f)
                            .WithEase(Ease.InOutExpo).BindToPosition(difficultyIndicatorCanvasGroup.transform)
                    ).Join(LMotion.Create(1f, 0f, 1f).WithEase(Ease.InOutExpo).BindToAlpha(resultLayer))
                    .Append(LMotion.Create(1f, 0f, 0.25f)
                        .WithOnComplete(() =>
                            {
                                SceneManager.LoadScene("Empty");
                                SceneManager.sceneLoaded += Register;
                                difficultyIndicatorCanvasGroup.transform.position = originalPosition;
                                _maidata.UnloadedResources();
                            }
                        )
                        .BindToAlpha(difficultyIndicatorCanvasGroup))
                    .Run()
            );

            Button.ClearAllMotion();
            Button.GetButton(4).Press();
            Button.HideAll(false);

            return;

            void Register(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= Register;

                LevelListController.GetInstance().gameObject.SetActive(false);
                LevelListController.GetInstance().gameObject.SetActive(true);

                SettingsController.Instance.RegisterEvent();

                LevelListController.GetInstance().ShowButton();
            }
        }

        private void ChangeAchievementVersion()
        {
            var currentValue = SettingsPool.GetValue("game.achievement_type");

            var targetValue = currentValue == 1 ? 0 : 1;

            SettingsPool.SetValue("game.achievement_type", targetValue);

            Button.GetButton(5).ClearMotion(true);
            Button.GetButton(5).Press();
            Button.GetButton(5).Hide(false, () =>
            {
                var buttonIconSet = UIManager.GetInstance().buttonIcons;

                Button.GetButton(5).ChangeIcon(targetValue == 0 ? buttonIconSet.finale : buttonIconSet.dx);
                Button.GetButton(5).Show(false);
            });

            SetScoreIndicatorContent();
        }

        private void ChangeScoreOrAchievement()
        {
            var currentValue = SettingsPool.GetValue("game.score_indicator_type");

            var targetValue = currentValue == 1 ? 0 : 1;

            SettingsPool.SetValue("game.score_indicator_type", targetValue);

            Button.GetButton(6).ClearMotion(true);
            Button.GetButton(6).Press();
            Button.GetButton(6).Hide(false, () =>
            {
                var buttonIconSet = UIManager.GetInstance().buttonIcons;

                Button.GetButton(6).ChangeIcon(targetValue == 0 ? buttonIconSet.score : buttonIconSet.achievement);
                Button.GetButton(6).Show();
            });

            SetScoreIndicatorContent();
        }

        private void SetScoreIndicatorContent()
        {
            achievementTitleText.text = SettingsPool.GetValue("game.score_indicator_type") switch
            {
                0 => "Score",
                _ => "Achievement"
            };

            var type = (AchievementType)SettingsPool.GetValue("game.achievement_type");

            achievementText.text = SettingsPool.GetValue("game.score_indicator_type") switch
            {
                0 => Scoreboard.GetScore().ToString(),
                _ => Scoreboard.GetCurrentAchievement(type)
                    .ToString(type == AchievementType.Finale ? "0.00" : "0.0000") + "%"
            };

            alternativeAchievementText.text = (type == AchievementType.Dx ? "F.A. " : "D.A. ") + Scoreboard
                .GetCurrentAchievement(type == AchievementType.Dx ? AchievementType.Finale : AchievementType.Dx)
                .ToString((type == AchievementType.Dx ? AchievementType.Finale : AchievementType.Dx) ==
                          AchievementType.Finale
                    ? "0.00"
                    : "0.0000") + "%";

            rankText.text = GetRankName(Scoreboard.GetCurrentAchievement(type), Scoreboard.GetScore(),
                Scoreboard.GetTotalScore(), type);

            comboText.text = Scoreboard.HighestCombo.ToString();
            comboStateText.text = Scoreboard.GetFcState() switch
            {
                FcState.Fc => "FC",
                FcState.FcGold => "FC",
                FcState.Ap => "AP",
                _ => "Played"
            };

            comboStateText.colorGradient = Scoreboard.GetFcState() switch
            {
                FcState.Fc => UIManager.GetInstance().fcColorGradient,
                FcState.FcGold => UIManager.GetInstance().fcGoldColorGradient,
                FcState.Ap => UIManager.GetInstance().fcGoldColorGradient,
                _ => UIManager.GetInstance().fcColorGradient
            };

            SaveHighestScore();
        }

        private void SaveHighestScore()
        {
            var chartRankData = ChartRankDataManager.GetChartRankData(_maidata.MaidataDirectoryName) ??
                                ChartRankDataManager.AddChartRankData(_maidata.MaidataDirectoryName);

            var levelRankData = chartRankData.GetLevelRankData(_difficultyIndex) ??
                                chartRankData.AddLevelRankData(_difficultyIndex);

            var finaleAchievement = Scoreboard.GetCurrentAchievement(AchievementType.Finale);
            var dxAchievement = Scoreboard.GetCurrentAchievement(AchievementType.Dx);

            if (finaleAchievement >
                levelRankData.LevelAchievements.FinaleBestAchievement.FinaleAchievement)
            {
                levelRankData.LevelAchievements.FinaleBestAchievement.FinaleAchievement = finaleAchievement;
                levelRankData.LevelAchievements.FinaleBestAchievement.DxAchievement = dxAchievement;
                levelRankData.LevelAchievements.FinaleBestAchievement.Score = Scoreboard.GetScore();
            }

            if (dxAchievement >
                levelRankData.LevelAchievements.DxBestAchievement.DxAchievement)
            {
                levelRankData.LevelAchievements.DxBestAchievement.FinaleAchievement = finaleAchievement;
                levelRankData.LevelAchievements.DxBestAchievement.DxAchievement = dxAchievement;
                levelRankData.LevelAchievements.DxBestAchievement.Score = Scoreboard.GetScore();
            }

            levelRankData.TotalScore = Scoreboard.GetTotalScore();

            levelRankData.Combo = Scoreboard.HighestCombo;

            var fcState = Scoreboard.GetFcState();

            if (fcState > levelRankData.FcState)
                levelRankData.FcState = fcState;

            ChartRankDataManager.Save();
        }

        private void SwitchDetailedScoreboard()
        {
            Button.GetButton(2).Press();

            _detailedScoreboardShown = !_detailedScoreboardShown;

            ClearMotion(true);

            if (_detailedScoreboardShown)
                AddMotionHandle(LSequence.Create()
                    .Append(LMotion.Create(0, 1f, 0.25f).WithEase(Ease.OutExpo)
                        .BindToAlpha(detailedScoreboardCanvasGroup))
                    .Join(LMotion
                        .Create(_detailedScoreboardPosition - Vector3.right * 5, _detailedScoreboardPosition, 0.25f)
                        .WithEase(Ease.OutExpo)
                        .BindToPosition(detailedScoreboardCanvasGroup.transform)
                    ).Run(), false);
            else
                AddMotionHandle(LSequence.Create()
                    .Append(LMotion.Create(1f, 0, 0.25f).WithEase(Ease.InExpo)
                        .BindToAlpha(detailedScoreboardCanvasGroup))
                    .Join(LMotion.Create(_detailedScoreboardPosition, _detailedScoreboardPosition - Vector3.right * 5,
                            0.25f).WithEase(Ease.InExpo)
                        .BindToPosition(detailedScoreboardCanvasGroup.transform)
                    ).Run(), false);
        }

        public static ResultController GetInstance()
        {
            return !_instance ? FindAnyObjectByType<ResultController>() : _instance;
        }

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
    }
}