using System;
using System.Collections;
using Game;
using UI.LevelSelection;
using UI.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.Result
{
    public class ResultManager : MonoBehaviour
    {
        private static ResultManager _instance;

        public VisualTreeAsset resultVisualTreeAsset;

        private SongCoverManipulator _backgroundSongCoverManipulator;
        private SongCoverManipulator _coverManipulator;

        private Maidata _maidata;

        private StyleSheet _preAnimatedStyleSheet;
        private VisualElement _resultPanel;

        private VisualElement _resultRoot;
        private StyleSheet _toRetryAnimatedStyleSheet;

        public static ResultManager Instance => _instance ??=
                                                FindObjectsByType<ResultManager>(FindObjectsInactive.Include,
                                                    FindObjectsSortMode.None)[^1];

        private void Awake()
        {
            _instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            _resultPanel.Q("background").RemoveManipulator(
                _backgroundSongCoverManipulator);
            _coverManipulator.OnGeometryChanged = null;
            _resultPanel.Q("song-cover-parent").RemoveManipulator(_coverManipulator);
            UIManager.Instance.uiDocument?.rootVisualElement?.Remove(_resultRoot);
        }

        private void Initialize()
        {
            ScreenOrientationManager.Instance.DisablePortrait();

            _maidata = ChartPlayer.Instance.Maidata;

            _resultRoot = resultVisualTreeAsset.Instantiate();

            UIManager.Instance.uiDocument.rootVisualElement.Add(_resultRoot);

            _resultRoot.style.position = new StyleEnum<Position>(Position.Absolute);
            _resultRoot.style.top = 0;
            _resultRoot.style.left = 0;
            _resultRoot.style.bottom = 0;
            _resultRoot.style.right = 0;

            _preAnimatedStyleSheet = Resources.Load<StyleSheet>("UI/USS/Result/GameToResultPreAnimated");

            _resultRoot.styleSheets.Add(_preAnimatedStyleSheet);

            _resultPanel = _resultRoot.Q("result-panel");

            var songCoverParent = _resultPanel.Q("song-cover-parent");

            var songCover = songCoverParent.Q("song-cover");

            var shade = songCoverParent.Q("song-cover-shade");
            shade.style.backgroundImage =
                _maidata.BlurredSongCoverDecodedImage.GetTexture2D();

            songCover.style.backgroundImage = _maidata.SongCoverDecodedImage.GetTexture2D();

            _coverManipulator =
                new SongCoverManipulator(SongCoverManipulator.SongCoverLayoutPopulationMode.MinimalLeft, -20);
            songCoverParent.AddManipulator(_coverManipulator);

            _coverManipulator.OnGeometryChanged += (sender, args) =>
            {
                songCover.style.backgroundSize = new StyleBackgroundSize(args.NewBackgroundSize);
            };

            var background = _resultPanel.Q("background");

            background.style.backgroundImage = _maidata.BlurredSongCoverAsBackgroundDecodedImage.GetTexture2D();

            _backgroundSongCoverManipulator =
                new SongCoverManipulator(SongCoverManipulator.SongCoverLayoutPopulationMode.FixedHeight, 0);

            background.AddManipulator(_backgroundSongCoverManipulator);

            _resultPanel.Q<Label>("player-name-label").text = DateTime.Now.ToString("g");

            SetEarlyLateData();
            SetSongData();
            SetStatsContainerData();
            SetTotalScorePanelData();
            SetScoreContentPanelData();
            UpdateRankData();

            Invoke(nameof(PlayInAnimation), 0.1f);

            var retryButton = _resultPanel.Q<Button>("retry-button");

            retryButton.clicked += () =>
            {
                _resultRoot.AddToClassList("in-animation");

                _resultRoot.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Result/ResultToRetryInAnimated"));

                SimulatedSensor.Clear();

                StartCoroutine(Retry());
            };

            var menuButton = _resultPanel.Q<Button>("get-back-button");

            menuButton.clicked += () =>
            {
                _resultPanel.AddToClassList("out-animation");
                _resultRoot.AddToClassList("dont-fade-out");

                _resultRoot.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Result/ResultToMenuInAnimated"));

                SimulatedSensor.Clear();

                StartCoroutine(LoadEmptyScene());
            };

            return;

            IEnumerator Retry()
            {
                yield return new WaitForSeconds(0.5f);

                LevelLoader.Instance.EnterLevel(_maidata, ChartPlayer.Instance.levelDifficultyIndex);
                LevelLoader.Instance.SceneLoaded += () =>
                {
                    StartCoroutine(PlayOutToRetryAnimation());

                    Destroy(UIManager.Instance.circleMaskManager.gameObject);
                };

                yield break;

                IEnumerator PlayOutToRetryAnimation()
                {
                    yield return new WaitForSeconds(0.1f);
                    _resultRoot.AddToClassList("out-animation");
                    _resultRoot.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Result/ResultToRetryOutAnimated"));
                    yield return new WaitForSeconds(0.5f);
                    RemoveThis();
                }
            }

            IEnumerator LoadEmptyScene()
            {
                yield return new WaitForSeconds(0.5f);
                SceneManager.LoadSceneAsync("Empty");
                SceneManager.sceneLoaded += LoadMenu;
            }
        }

        private void LoadMenu(Scene arg1, LoadSceneMode arg2)
        {
            SceneManager.sceneLoaded -= LoadMenu;

            Destroy(UIManager.Instance.circleMaskManager.gameObject);

            _maidata.UnloadResources();

            UIManager.Instance.ShowLevelSelector();

            _resultRoot.BringToFront();

            var levelSelectionPreAnimatedSheet =
                Resources.Load<StyleSheet>("UI/USS/LevelSelection/ResultToLevelSelectionPreAnimated");
            UIManager.Instance.levelSelectionManager.LevelSelectionTree.styleSheets
                .Add(levelSelectionPreAnimatedSheet);

            UIManager.Instance.levelSelectionManager.LevelSelectionTree.AddToClassList("hide-selected");

            StartCoroutine(PlayOutToLevelMenuAnimation());

            return;

            IEnumerator PlayOutToLevelMenuAnimation()
            {
                yield return new WaitForSeconds(0.1f);
                _resultRoot.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Result/ResultToMenuOutAnimated"));

                yield return new WaitForSeconds(0.5f);
                UIManager.Instance.levelSelectionManager.LevelSelectionTree.styleSheets
                    .Remove(levelSelectionPreAnimatedSheet);
                UIManager.Instance.levelSelectionManager.LevelSelectionTree.AddToClassList("out-animation");
                UIManager.Instance.levelSelectionManager.LevelSelectionTree
                    .AddToClassList("no-opacity-transition");

                yield return new WaitForSeconds(0.5f);
                UIManager.Instance.levelSelectionManager.LevelSelectionTree.RemoveFromClassList("out-animation");
                UIManager.Instance.levelSelectionManager.LevelSelectionTree.RemoveFromClassList("hide-selected");
                _resultRoot.style.opacity = 0;

                yield return new WaitForSeconds(0.1f);
                UIManager.Instance.levelSelectionManager.LevelSelectionTree
                    .RemoveFromClassList("no-opacity-transition");
                RemoveThis();
            }
        }

        private void RemoveThis()
        {
            Destroy(gameObject);
        }

        private void SetEarlyLateData()
        {
            var panel = _resultPanel.Q("early-late-panel");
            var late = panel.Q<Label>("late-label");
            var early = panel.Q<Label>("early-label");

            late.text = Scoreboard.LateCount.ToString();
            early.text = Scoreboard.FastCount.ToString();
        }

        private void PlayInAnimation()
        {
            _resultRoot.styleSheets.Remove(_preAnimatedStyleSheet);

            _resultRoot.AddToClassList("out-animation");

            StartCoroutine(RemoveAnimationClass());

            return;

            IEnumerator RemoveAnimationClass()
            {
                yield return new WaitForSeconds(0.5f);
                _resultRoot.RemoveFromClassList("out-animation");
            }
        }

        private void SetSongData()
        {
            var element = _resultPanel.Q("song-item-container");

            var informationElement = element.Q<VisualElement>("song-item").Q<VisualElement>("information");

            var songTitleWaterMark = element.Q<VisualElement>("song-item").Q<Label>("song-title-watermark");
            var songTitleLabel = informationElement.Q<Label>("song-title");

            songTitleLabel.text = _maidata.Title;

            songTitleWaterMark.text =
                $"<color=white><gradient=\"level-item-watermark\">{_maidata.Title}</gradient></color>";

            var artistLabel = informationElement.Q<Label>("song-artist");
            var bpmLabel = informationElement.Q<Label>("song-bpm");

            artistLabel.text = _maidata.Artist;
            bpmLabel.text = _maidata.Bpm.ToString("");
        }

        private void SetStatsContainerData()
        {
            var statsContainer = _resultPanel.Q("stats-container");

            for (var i = 1; i <= 5; i++)
            {
                var row = statsContainer.Q($"row-{i}");

                var specifiedScoreboard = i switch
                {
                    1 => new SpecifiedNoteScoreboard(
                        Scoreboard.TapCount.TotalCount + Scoreboard.HoldCount.TotalCount +
                        Scoreboard.SlideCount.TotalCount + Scoreboard.BreakCount.TotalCount,
                        Scoreboard.TapCount.CriticalPerfectCount + Scoreboard.HoldCount.CriticalPerfectCount +
                        Scoreboard.SlideCount.CriticalPerfectCount + Scoreboard.BreakCount.CriticalPerfectCount,
                        Scoreboard.TapCount.SemiCriticalPerfectCount + Scoreboard.HoldCount.SemiCriticalPerfectCount +
                        Scoreboard.SlideCount.SemiCriticalPerfectCount + Scoreboard.BreakCount.SemiCriticalPerfectCount,
                        Scoreboard.TapCount.PerfectCount + Scoreboard.HoldCount.PerfectCount +
                        Scoreboard.SlideCount.PerfectCount + Scoreboard.BreakCount.PerfectCount,
                        Scoreboard.TapCount.GreatCount + Scoreboard.HoldCount.GreatCount +
                        Scoreboard.SlideCount.GreatCount + Scoreboard.BreakCount.GreatCount,
                        Scoreboard.TapCount.SemiGreatCount + Scoreboard.HoldCount.SemiGreatCount +
                        Scoreboard.SlideCount.SemiGreatCount + Scoreboard.BreakCount.SemiGreatCount,
                        Scoreboard.TapCount.QuarterGreatCount + Scoreboard.HoldCount.QuarterGreatCount +
                        Scoreboard.SlideCount.QuarterGreatCount + Scoreboard.BreakCount.QuarterGreatCount,
                        Scoreboard.TapCount.GoodCount + Scoreboard.HoldCount.GoodCount +
                        Scoreboard.SlideCount.GoodCount + Scoreboard.BreakCount.GoodCount,
                        Scoreboard.TapCount.MissCount + Scoreboard.HoldCount.MissCount +
                        Scoreboard.SlideCount.MissCount + Scoreboard.BreakCount.MissCount
                    ),
                    2 => Scoreboard.TapCount,
                    3 => Scoreboard.HoldCount,
                    4 => Scoreboard.SlideCount,
                    5 => Scoreboard.BreakCount,
                    _ => Scoreboard.TapCount
                };

                for (var j = 1; j <= 4; j++)
                {
                    var label = row.Q<Label>($"label-{j}");

                    var count = j switch
                    {
                        1 => specifiedScoreboard.MissCount,
                        2 => specifiedScoreboard.GoodCount,
                        3 => specifiedScoreboard.GreatCount + specifiedScoreboard.SemiGreatCount +
                             specifiedScoreboard.QuarterGreatCount,
                        4 => specifiedScoreboard.CriticalPerfectCount + specifiedScoreboard.SemiCriticalPerfectCount +
                             specifiedScoreboard.PerfectCount,
                        _ => specifiedScoreboard.PerfectCount
                    };

                    label.text = $"{count}";
                }
            }
        }

        private void SetTotalScorePanelData()
        {
            var totalScoreContainer = _resultPanel.Q("total-score-container");

            totalScoreContainer.Q<Label>("score-label").text = Scoreboard.GetScore().ToString();

            var scoreRow = totalScoreContainer.Q("row-1");

            scoreRow.Q<Label>("label-1").text = Scoreboard.BreakCount.PerfectCount.ToString();
            scoreRow.Q<Label>("label-2").text = Scoreboard.BreakCount.SemiCriticalPerfectCount.ToString();
            scoreRow.Q<Label>("label-3").text = Scoreboard.BreakCount.CriticalPerfectCount.ToString();
        }

        private void SetScoreContentPanelData()
        {
            var chartRankData = new ChartRankData("temp-rank");
            chartRankData.AddLevelRankData(ChartPlayer.Instance.levelDifficultyIndex);

            var levelRankData = chartRankData.GetLevelRankData(ChartPlayer.Instance.levelDifficultyIndex);
            levelRankData.FcState = Scoreboard.GetFcState();
            levelRankData.Combo = Scoreboard.HighestCombo;
            levelRankData.TotalScore = Scoreboard.GetTotalScore();

            var scorePair = new LevelAchievement.ScorePair
            {
                DxAchievement = Scoreboard.GetCurrentAchievement(AchievementType.Dx),
                FinaleAchievement = Scoreboard.GetCurrentAchievement(AchievementType.Finale),
                Score = Scoreboard.GetScore()
            };

            levelRankData.LevelAchievements.DxBestAchievement = scorePair;
            levelRankData.LevelAchievements.FinaleBestAchievement = scorePair;

            var scoreContentPanel = _resultPanel.Q<ScoreContentPanel>();

            scoreContentPanel.SetChartInformationData(_maidata, ChartPlayer.Instance.levelDifficultyIndex);
            scoreContentPanel.SetScoreData(chartRankData, ChartPlayer.Instance.levelDifficultyIndex);
        }

        private void UpdateRankData()
        {
            var autoEnabled = SettingsPool.GetValue("auto_play") == 1;

            if (autoEnabled)
                return;

            var chartRankData = ChartRankDataManager.GetChartRankData(_maidata.MaidataDirectoryName) ??
                                ChartRankDataManager.AddChartRankData(_maidata.MaidataDirectoryName);
            var levelRankData = chartRankData.GetLevelRankData(ChartPlayer.Instance.levelDifficultyIndex) ??
                                chartRankData.AddLevelRankData(ChartPlayer.Instance.levelDifficultyIndex);

            if (Scoreboard.GetFcState() >= levelRankData.FcState) levelRankData.FcState = Scoreboard.GetFcState();

            var finaleAchievement = Scoreboard.GetCurrentAchievement(AchievementType.Finale);
            var dxAchievement = Scoreboard.GetCurrentAchievement(AchievementType.Dx);

            if (finaleAchievement >=
                levelRankData.LevelAchievements.FinaleBestAchievement.FinaleAchievement)
            {
                levelRankData.LevelAchievements.FinaleBestAchievement.FinaleAchievement =
                    finaleAchievement;
                levelRankData.LevelAchievements.FinaleBestAchievement.DxAchievement =
                    dxAchievement;

                levelRankData.LevelAchievements.FinaleBestAchievement.Score = Scoreboard.GetScore();
            }

            if (dxAchievement >=
                levelRankData.LevelAchievements.DxBestAchievement.DxAchievement)
            {
                levelRankData.LevelAchievements.DxBestAchievement.FinaleAchievement =
                    finaleAchievement;
                levelRankData.LevelAchievements.DxBestAchievement.DxAchievement =
                    dxAchievement;

                levelRankData.LevelAchievements.DxBestAchievement.Score = Scoreboard.GetScore();
            }

            if (Scoreboard.HighestCombo >= levelRankData.Combo)
                levelRankData.Combo = Scoreboard.HighestCombo;

            levelRankData.TotalScore = Scoreboard.GetTotalScore();

            ChartRankDataManager.Save();
        }
    }
}