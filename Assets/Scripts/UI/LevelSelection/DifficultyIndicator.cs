using System.Collections;
using System.Threading.Tasks;
using Game;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UI.GameSettings;
using UI.Result;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.LevelSelection
{
    public class DifficultyIndicator : UIScriptWithAnimation
    {
        private static DifficultyIndicator _instance;
        public Color[] backgroundColors;
        public Color[] textColors;

        public VertexGradient[] textGradientColors;

        public Image backgroundImage;

        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI charterNameText;
        public TextMeshProUGUI difficultyNameText;
        public TextMeshProUGUI achievementTitleText;
        public TextMeshProUGUI achievementText;
        public TextMeshProUGUI alternativeAchievementText;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI rankTitleText;

        public TextMeshProUGUI comboText;
        public TextMeshProUGUI comboStateText;

        [FormerlySerializedAs("canvasGroup")] public CanvasGroup difficultyIndicatorCanvasGroup;
        public CanvasGroup comboIndicatorCanvasGroup;

        public CanvasGroup scoreIndicatorCanvasGroup;

        private Maidata _lastSelectedMaidata;
        private Vector3 _originalDifficultyIndicatorPosition;

        private Vector3 _originalListPosition;

        private int _selectedDifficulty;

        private Coroutine _songPlaybackCoroutine;

        private AudioSourcePool.AudioSourceHandler _songPreviewAudioSourceHandler;

        private void OnEnable()
        {
            LevelListController.GetInstance().levelList.OnItemSelected += OnLevelSelected;

            SimulatedSensor.OnTap += OnTap;

            _instance = this;

            if (_songPlaybackCoroutine != null)
                _songPlaybackCoroutine = StartCoroutine(WaitAndPlaySong());
        }

        private void OnDisable()
        {
            SimulatedSensor.OnTap -= OnTap;
            LevelListController.GetInstance().levelList.OnItemSelected -= OnLevelSelected;

            if (_songPlaybackCoroutine != null && _songPreviewAudioSourceHandler != null)
            {
                StopCoroutine(_songPlaybackCoroutine);
                LMotion.Create(SettingsPool.GetValue("game.volume.song") / 10f, 0f, 0.2f).WithOnComplete(() =>
                {
                    _songPreviewAudioSourceHandler.Stop();
                    _songPreviewAudioSourceHandler = null;
                }).Bind(x => _songPreviewAudioSourceHandler.SetVolume(x));
            }
        }

        public static DifficultyIndicator GetInstance()
        {
            return _instance ??
                   FindAnyObjectByType<DifficultyIndicator>(
                       FindObjectsInactive.Include);
        }

        private void OnTap(object _, TouchEventArgs args)
        {
            if (args.SensorId == "A2")
                ChangeDifficulty(-1);
            if (args.SensorId == "A3")
                ChangeDifficulty(1);
            if (args.SensorId == "A5")
            {
                var isHolding = LevelListController.GetInstance().levelList.isHolding;

                LevelListController.GetInstance().levelList.EndHoldingUp();
                LevelListController.GetInstance().levelList.EndHoldingDown();

                if (isHolding)
                    return;

                ClearMotion(true);

                SimulatedSensor.OnTap = null;
                SimulatedSensor.OnHold = null;
                SimulatedSensor.OnLeave = null;

                StartCoroutine(EnterLevel());
            }
        }

        private IEnumerator EnterLevel()
        {
            if (_songPreviewAudioSourceHandler != null)
                LMotion.Create(SettingsPool.GetValue("game.volume.song") / 10f, 0f, 0.2f).WithOnComplete(() =>
                {
                    _songPreviewAudioSourceHandler.Stop();
                    _songPreviewAudioSourceHandler = null;
                }).Bind(x => _songPreviewAudioSourceHandler.SetVolume(x));

            if (_songPlaybackCoroutine != null) StopCoroutine(_songPlaybackCoroutine);

            Scoreboard.Reset();

            Button.ClearAllMotion();
            Button.GetButton(4).Press();
            Button.HideAll(false);

            var levelListController = LevelListController.GetInstance();

            var difficultyIndex =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .DifficultyIndex;

            var maidata =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .Maidata;

            if (!maidata.SongLoaded || !maidata.BlurredSongCoverGenerated)
            {
                Task.Run(() =>
                {
                    maidata.GenerateBlurredCover();
                    if (!maidata.CoverDataLoaded)
                        maidata.LoadSongCover();
                });

                if (!maidata.SongLoaded && !maidata.LoadingSong)
                    StartCoroutine(maidata.LoadSongClip());
            }

            while (true)
            {
                yield return null;

                if (!maidata.SongLoaded || maidata.BlurredSongCoverAsBackgroundDecodedImage == null ||
                    maidata.SongCoverDecodedImage == null)
                    continue;

                break;
            }

            _originalListPosition = levelListController.levelList.transform.position;
            _originalDifficultyIndicatorPosition = transform.position;

            levelListController.songCoverBackgroundImage.sprite = SettingsPool.GetValue("game.blurred_cover") == 1
                ? maidata.BlurredSongCoverAsBackgroundDecodedImage.GetSprite()
                : maidata.SongCoverDecodedImage.GetSprite();

            levelListController.songCoverBackgroundImage.transform.localScale =
                SettingsPool.GetValue("game.blurred_cover") == 1 ? Vector3.one * 1.1f : Vector3.one;

            AddMotionHandle(LSequence.Create()
                .Append(LMotion.Create(0, -15f, 0.5f).WithEase(Ease.InExpo).Bind(x =>
                    levelListController.levelList.transform.position = _originalListPosition + new Vector3(x, 0, 0)))
                .Join(
                    LMotion.Create(0, 5f, 0.5f).WithEase(Ease.InExpo)
                        .Bind(x => transform.position = _originalDifficultyIndicatorPosition + new Vector3(x, 0, 0))
                ).Join(
                    LMotion.Create(0, 1f, 0.5f).WithEase(Ease.InExpo).WithOnComplete(() =>
                        {
                            LoadScene(maidata.SongAudioClip, maidata, difficultyIndex);
                            levelListController.backgroundImage.enabled = false;
                        })
                        .BindToColorA(levelListController.songCoverBackgroundImage)
                ).Run());
        }

        public void ReloadScene()
        {
            var levelListController = LevelListController.GetInstance();

            var difficultyIndex =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .DifficultyIndex;

            var maidata =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .Maidata;

            levelListController.levelSelectionUiLayer.gameObject.SetActive(true);
            levelListController.levelSelectionUiLayer.alpha = 0;

            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnLeave = null;

            LoadScene(maidata.SongAudioClip, maidata, difficultyIndex);
        }

        private void LoadScene(AudioClip clip, Maidata maidata, int difficultyIndex)
        {
            SceneManager.LoadScene("Game");

            SceneManager.sceneLoaded += OnSceneLoaded;

            return;

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                var noteGenerator =
                    FindAnyObjectByType<NoteGenerator>(FindObjectsInactive.Include);
                noteGenerator.GenerateNotes(maidata.Charts[difficultyIndex], maidata.FirstNoteTime);

                var chartPlayer = FindAnyObjectByType<ChartPlayer>(FindObjectsInactive.Include);

                chartPlayer.songClip = clip;

                chartPlayer.backgroundImage.texture = SettingsPool.GetValue("game.blurred_cover") == 1
                    ? maidata.BlurredSongCoverAsBackgroundDecodedImage.GetTexture2D()
                    : maidata.SongCoverDecodedImage.GetTexture2D();

                chartPlayer.backgroundImage.transform.localScale =
                    SettingsPool.GetValue("game.blurred_cover") == 1
                        ? 1.1f * Vector3.one
                        : Vector3.one;

                chartPlayer.LoadVideo(maidata.PvPath);

                chartPlayer.InitializeCircleColor(difficultyIndex, maidata.IsUtage);

                var resultController = ResultController.GetInstance();

                resultController.Initialize(maidata, difficultyIndex);

                StartCoroutine(PlayChart(chartPlayer));
            }
        }

        private IEnumerator PlayChart(ChartPlayer chartPlayer)
        {
            for (var i = 0; i < 4; i++) yield return new WaitForEndOfFrame();

            UIManager.GetInstance().mask.showMaskGraphic = false;

            AddMotionHandle(LMotion.Create(1, 0f, 0.5f).WithEase(Ease.OutExpo).WithOnComplete(() =>
                {
                    LevelListController.GetInstance().levelList.transform.position = _originalListPosition;
                    transform.position = _originalDifficultyIndicatorPosition;
                    chartPlayer.Play();
                    UIManager.GetInstance().DisableUI();
                    LevelListController.GetInstance().backgroundImage.enabled = true;
                    LevelListController.GetInstance().levelSelectionUiLayer.alpha = 1;
                    LevelListController.GetInstance().songCoverBackgroundImage.color = new Color(
                        LevelListController.GetInstance().songCoverBackgroundImage.color.r,
                        LevelListController.GetInstance().songCoverBackgroundImage.color.g,
                        LevelListController.GetInstance().songCoverBackgroundImage.color.b, 0);
                })
                .Bind(x => { UIManager.GetInstance().maskCanvasGroup.alpha = x; }));
        }

        private void ChangeDifficulty(int direction)
        {
            var list = LevelListController.GetInstance().levelList;

            if (list.AllData[list.dataIndex] is not LevelListItemData currentLevelListItemData)
                return;

            var currentDifficultyIndex = currentLevelListItemData.DifficultyIndex;

            var nextDifficultyIndex = currentDifficultyIndex;
            switch (direction)
            {
                case 1:
                    for (var j = currentDifficultyIndex + 1;
                         j < currentLevelListItemData.Maidata.Difficulties.Length;
                         j++)
                    {
                        if (currentLevelListItemData.Maidata.Charts[j] == "") continue;
                        nextDifficultyIndex = j;
                        break;
                    }

                    break;
                case -1:
                    for (var j = currentDifficultyIndex - 1; j > -1; j--)
                    {
                        if (currentLevelListItemData.Maidata.Charts[j] == "") continue;

                        nextDifficultyIndex = j;
                        break;
                    }

                    break;
            }

            if (nextDifficultyIndex != currentDifficultyIndex)
            {
                var button = direction switch
                {
                    -1 => Button.GetButton(1),
                    _ => Button.GetButton(2)
                };

                button.Press();
            }

            if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Difficulty)
            {
                var targetIndex = list.dataIndex;

                for (var i = 0; i < list.AllData.Length; i++)
                {
                    var listItemData = list.AllData[i];
                    if (listItemData is not LevelListItemData levelListItemData)
                        continue;

                    if (levelListItemData.Maidata == currentLevelListItemData.Maidata &&
                        levelListItemData.DifficultyIndex == nextDifficultyIndex)
                    {
                        targetIndex = i;

                        break;
                    }
                }

                if (currentDifficultyIndex == ((LevelListItemData)list.AllData[targetIndex]).DifficultyIndex)
                    return;

                list.MoveTo(targetIndex);
            }
            else if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Alphabet)
            {
                var lastSelectedDifficulty = currentLevelListItemData.DifficultyIndex;

                _selectedDifficulty = nextDifficultyIndex;

                currentLevelListItemData.DifficultyIndex = _selectedDifficulty;

                if (lastSelectedDifficulty != _selectedDifficulty)
                    OnLevelSelected(LevelListController.GetInstance().levelList,
                        new ListEventArgs(LevelListController.GetInstance().levelList.dataIndex, true));
            }
        }

        public void SetScoreIndicatorContent(string maidataPath, int difficultyIndex)
        {
            achievementTitleText.text = SettingsPool.GetValue("game.score_indicator_type") switch
            {
                0 => "Score",
                _ => "Achievement"
            };

            scoreIndicatorCanvasGroup.alpha = 1;

            var type = (ResultController.AchievementType)SettingsPool.GetValue("game.achievement_type");

            var chartRankData = ChartRankDataManager.GetChartRankData(maidataPath);

            if (chartRankData == null)
            {
                scoreIndicatorCanvasGroup.alpha = 0;
                return;
            }

            var levelRankData = chartRankData.GetLevelRankData(difficultyIndex);

            if (levelRankData == null)
            {
                scoreIndicatorCanvasGroup.alpha = 0;
                return;
            }

            var score = type switch
            {
                ResultController.AchievementType.Dx => levelRankData.LevelAchievements.DxBestAchievement.Score,
                _ => levelRankData.LevelAchievements.FinaleBestAchievement.Score
            };

            var achievement = type switch
            {
                ResultController.AchievementType.Dx => levelRankData.LevelAchievements.DxBestAchievement
                    .DxAchievement,
                _ => levelRankData.LevelAchievements.FinaleBestAchievement.FinaleAchievement
            };

            achievementText.text = SettingsPool.GetValue("game.score_indicator_type") switch
            {
                0 => score.ToString(),
                _ => achievement.ToString(type == ResultController.AchievementType.Finale ? "0.00" : "0.0000") + "%"
            };

            alternativeAchievementText.text = type switch
            {
                ResultController.AchievementType.Finale => "D.A. " + levelRankData.LevelAchievements
                    .FinaleBestAchievement
                    .DxAchievement.ToString("0.0000") + "%",
                _ => "F.A. " + levelRankData.LevelAchievements.DxBestAchievement.FinaleAchievement.ToString("0.00") +
                     "%"
            };

            rankText.text = ResultController.GetRankName(achievement, score,
                levelRankData.TotalScore, type);

            comboText.text = levelRankData.Combo.ToString();
            comboStateText.text = levelRankData.FcState switch
            {
                FcState.Fc => "FC",
                FcState.FcGold => "FC",
                FcState.Ap => "AP",
                _ => "Played"
            };

            comboStateText.colorGradient = levelRankData.FcState switch
            {
                FcState.Fc => UIManager.GetInstance().fcColorGradient,
                FcState.FcGold => UIManager.GetInstance().fcGoldColorGradient,
                FcState.Ap => UIManager.GetInstance().fcGoldColorGradient,
                _ => UIManager.GetInstance().fcColorGradient
            };
        }

        private IEnumerator WaitAndPlaySong()
        {
            if (_songPreviewAudioSourceHandler != null)
                LMotion.Create(SettingsPool.GetValue("game.volume.song") / 10f, 0f, 0.2f).WithOnComplete(() =>
                {
                    if (_songPreviewAudioSourceHandler != null)
                    {
                        _songPreviewAudioSourceHandler.Stop();
                        _songPreviewAudioSourceHandler = null;
                    }
                }).Bind(x =>
                {
                    if (_songPreviewAudioSourceHandler != null) _songPreviewAudioSourceHandler.SetVolume(x);
                });

            yield return new WaitForSeconds(0.5f);

            var levelListController = LevelListController.GetInstance();

            var maidata =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .Maidata;

            if (!maidata.SongLoaded || !maidata.BlurredSongCoverGenerated)
                if (!maidata.SongLoaded && !maidata.LoadingSong)
                    StartCoroutine(maidata.LoadSongClip());

            while (true)
            {
                yield return null;

                if (!maidata.SongLoaded)
                    continue;

                break;
            }

            AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out _songPreviewAudioSourceHandler);

            _songPreviewAudioSourceHandler.SetClip(maidata.SongAudioClip);
            _songPreviewAudioSourceHandler.Play();
            _songPreviewAudioSourceHandler.SetVolume(SettingsPool.GetValue("game.volume.song") / 10f);

            while (true)
            {
                yield return null;

                if (_songPreviewAudioSourceHandler.IsFree)
                {
                    AudioManager.GetInstance().AudioSourcePool
                        .TryGetAudioSourceHandler(out _songPreviewAudioSourceHandler);

                    _songPreviewAudioSourceHandler.SetClip(maidata.SongAudioClip);
                    _songPreviewAudioSourceHandler.Play();
                    _songPreviewAudioSourceHandler.SetVolume(SettingsPool.GetValue("game.volume.song") / 10f);
                }
            }
        }

        private void OnLevelSelected(object sender, ListEventArgs e)
        {
            if (sender is not List list)
                return;

            var selectedItemData = list.AllData[e.Index];

            if (selectedItemData is not LevelListItemData levelListItemData)
                return;

            if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Alphabet)
            {
                while (levelListItemData.Maidata.Charts[_selectedDifficulty] == string.Empty)
                {
                    if (_selectedDifficulty < 1)
                        _selectedDifficulty = 6;
                    _selectedDifficulty -= 1;
                }

                levelListItemData.DifficultyIndex = _selectedDifficulty;
            }

            backgroundImage.color =
                backgroundColors[levelListItemData.Maidata.IsUtage ? 5 : levelListItemData.DifficultyIndex];

            difficultyText.text = levelListItemData.Maidata.Difficulties[levelListItemData.DifficultyIndex];

            var designerName = levelListItemData.Maidata.Designers[levelListItemData.DifficultyIndex];

            designerName = designerName == "\r" ? levelListItemData.Maidata.MainChartDesigner : designerName;
            designerName = designerName == "\r" ? "Unknown Designer" : designerName;

            charterNameText.text = designerName;

            difficultyNameText.text = (levelListItemData.Maidata.IsUtage ? 6 : levelListItemData.DifficultyIndex) switch
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

            SetScoreIndicatorContent(levelListItemData.Maidata.MaidataDirectoryName, levelListItemData.DifficultyIndex);

            var textColor = textColors[levelListItemData.Maidata.IsUtage ? 5 : levelListItemData.DifficultyIndex];

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
                textGradientColors[levelListItemData.Maidata.IsUtage ? 5 : levelListItemData.DifficultyIndex];
            backgroundImage.color =
                backgroundColors[levelListItemData.Maidata.IsUtage ? 5 : levelListItemData.DifficultyIndex];

            if (e.IndexChangeIsAnimated)
            {
                AddMotionHandle(LMotion.Create(0, 1f, 0.5f).WithEase(Ease.OutExpo).Bind(x =>
                {
                    difficultyIndicatorCanvasGroup.alpha = x;
                    transform.position = new Vector3(transform.position.x,
                        LevelListController.GetInstance().levelList.GetSelectedItemObject().transform.position.y,
                        transform.position.z);
                }));
            }
            else
            {
                difficultyIndicatorCanvasGroup.alpha = 1;
                transform.position = new Vector3(transform.position.x,
                    LevelListController.GetInstance().levelList.GetSelectedItemObject().transform.position.y,
                    transform.position.z);
            }

            if (_lastSelectedMaidata != levelListItemData.Maidata)
            {
                if (_songPlaybackCoroutine != null)
                    StopCoroutine(_songPlaybackCoroutine);
                _songPlaybackCoroutine = StartCoroutine(WaitAndPlaySong());
            }

            _lastSelectedMaidata = levelListItemData.Maidata;
        }
    }
}