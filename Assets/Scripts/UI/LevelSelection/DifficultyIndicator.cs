using System;
using System.Collections;
using System.IO;
using Game;
using LitMotion;
using TMPro;
using UI.GameSettings;
using UI.Result;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.LevelSelection
{
    public class DifficultyIndicator : UIScriptWithAnimation
    {
        public static DifficultyIndicator Instance;
        public Color[] backgroundColors;
        public Color[] textColors;

        public VertexGradient[] textGradientColors;

        public Image backgroundImage;

        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI charterNameText;
        public TextMeshProUGUI difficultyNameText;

        public CanvasGroup canvasGroup;

        public RectTransform mask;

        private int _selectedDifficulty;

        private void OnEnable()
        {
            LevelListController.GetInstance().levelList.OnItemSelected += OnLevelSelected;
            
            SimulatedSensor.OnTap += OnTap;

            var levelList = LevelListController.GetInstance().levelList;
            
            if (_chartLoadingRoutine != null)
                StopCoroutine(_chartLoadingRoutine);
            if (levelList.index > -1 && levelList.index < levelList.ItemObjectList.Count)
                _chartLoadingRoutine = StartCoroutine(
                    LoadChartResource(((LevelListItem)levelList
                        .ItemObjectList[levelList.index]).maidata));

            Instance = this;
        }

        private void OnTap(object _,TouchEventArgs args)
        {
            if (args.SensorId == "A2")
                ChangeDifficulty(-1);
            if (args.SensorId == "A3")
                ChangeDifficulty(1);
            if (args.SensorId == "A5")
                EnterLevel();
        }

        private void OnDisable()
        {
            SimulatedSensor.OnTap -= OnTap;
            LevelListController.GetInstance().levelList.OnItemSelected -= OnLevelSelected;
        }

        private void EnterLevel()
        {
            var levelListController = LevelListController.GetInstance();

            var difficultyIndex =
                ((LevelListItem)levelListController.levelList.ItemObjectList[levelListController.levelList.index])
                .difficultyIndex;

            var maidata =
                ((LevelListItem)levelListController.levelList.ItemObjectList[levelListController.levelList.index])
                .maidata;

            if (!maidata.SongLoaded || !maidata.BlurredSongCoverGenerated)
                return;
            
            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            var originalListPosition = levelListController.levelList.transform.position;
            var originalPosition = transform.position;

            var currentAlpha = levelListController.songCoverBackgroundImage.color.a;

            levelListController.songCoverBackgroundImage.sprite = SettingsPool.GetValue("game.blurred_cover") == 1
                ? maidata.SongCoverBlurredAsBackground
                : maidata.SongCover;

            levelListController.songCoverBackgroundImage.transform.localScale =
                SettingsPool.GetValue("game.blurred_cover") == 1 ? Vector3.one * 1.1f : Vector3.one;

            AddMotionHandle(LMotion.Create(
                    0, 15f, 0.5f).WithEase(Ease.InExpo).WithOnComplete(() =>
                {
                    LoadScene(maidata.SongAudioClip, maidata, difficultyIndex);
                    levelListController.backgroundImage.enabled = false;
                })
                .Bind(x =>
                {
                    levelListController.levelList.transform.position = originalListPosition + new Vector3(-x, 0, 0);
                    var rgbValue = (1 - (x / 15f)) * 0.43529412f + 0.56470588f;
                    levelListController.songCoverBackgroundImage.color =
                        new Color(rgbValue, rgbValue, rgbValue, (x / 15f) * (1 - currentAlpha) + currentAlpha);
                    transform.position = originalPosition + new Vector3(x * 0.3f, 0, 0);
                })
            );
        }

        private void LoadScene(AudioClip audioClip, Maidata maidata, int difficultyIndex)
        {
            SceneManager.LoadScene("Game");

            SceneManager.sceneLoaded += (_, _) =>
            {
                var noteGenerator =
                    FindAnyObjectByType<NoteGenerator>(FindObjectsInactive.Include);
                noteGenerator.GenerateNotes(maidata.Charts[difficultyIndex], maidata.FirstNoteTime);

                var chartPlayer = FindAnyObjectByType<ChartPlayer>(FindObjectsInactive.Include);

                chartPlayer.audioSource.clip = audioClip;

                chartPlayer.backgroundImage.sprite = SettingsPool.GetValue("game.blurred_cover") == 1
                    ? maidata.SongCoverBlurredAsBackground
                    : maidata.SongCover;

                chartPlayer.backgroundImage.transform.localScale =
                    SettingsPool.GetValue("game.blurred_cover") == 1
                        ? 1.1f * Vector3.one
                        : Vector3.one;

                chartPlayer.InitializeCircleColor(difficultyIndex, maidata.IsUtage);

                var resultController = ResultController.GetInstance();

                resultController.Initialize(maidata, difficultyIndex);

                StartCoroutine(PlayChart(chartPlayer));
            };
        }

        private IEnumerator PlayChart(ChartPlayer chartPlayer)
        {
            for (var i = 0; i < 4; i++) yield return new WaitForEndOfFrame();

            while (true)
                if (chartPlayer.audioSource.clip.loadState == AudioDataLoadState.Loaded)
                    break;

            AddMotionHandle(LMotion.Create(1, 0f, 0.5f).WithEase(Ease.OutExpo).WithOnComplete(() =>
                {
                    LevelListController.GetInstance().levelList.transform.position += new Vector3(15, 0, 0);
                    transform.position += new Vector3(-5, 0, 0);
                    StartCoroutine(WaitAndPlay());
                    LevelListController.GetInstance().backgroundImage.enabled = true;
                })
                .Bind(x => { UIManager.Instance.maskCanvasGroup.alpha = x; }));

            yield break;

            IEnumerator WaitAndPlay()
            {
                yield return new WaitForSeconds(1);

                chartPlayer.Play();
                UIManager.Instance.DisableUI();
            }
        }

        private void ChangeDifficulty(int direction)
        {
            if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Difficulty)
            {
                var list = LevelListController.GetInstance().levelList;

                if (list.ItemObjectList[list.index] is not LevelListItem currentLevelListItem)
                    return;

                var currentDifficultyIndex = currentLevelListItem.difficultyIndex;

                var targetIndex = list.index;
                for (var i = 0; i < list.ItemObjectList.Count; i++)
                {
                    var listItem = list.ItemObjectList[i];
                    if (listItem is not LevelListItem levelListItem)
                        continue;

                    if (levelListItem.maidata == currentLevelListItem.maidata &&
                        levelListItem.difficultyIndex == currentDifficultyIndex + direction)
                    {
                        targetIndex = i;
                        break;
                    }
                }

                list.MoveTo(targetIndex);
            }
            else if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Alphabet)
            {
                var list = LevelListController.GetInstance().levelList;

                if (list.ItemObjectList[list.index] is not LevelListItem currentLevelListItem)
                    return;

                var lastSelectedDifficulty = currentLevelListItem.difficultyIndex;

                if (!((_selectedDifficulty == 5 && direction == 1) || (_selectedDifficulty == 0 && direction == -1)))
                    _selectedDifficulty += direction;

                if (currentLevelListItem.maidata.Difficulties[_selectedDifficulty] == string.Empty &&
                    currentLevelListItem.maidata.Designers[_selectedDifficulty] == string.Empty &&
                    currentLevelListItem.maidata.Charts[_selectedDifficulty] == string.Empty)
                    _selectedDifficulty -= direction;

                currentLevelListItem.difficultyIndex = _selectedDifficulty;

                if (lastSelectedDifficulty != _selectedDifficulty)
                    OnLevelSelected(LevelListController.GetInstance().levelList,
                        new ListEventArgs(LevelListController.GetInstance().levelList.index, true));
            }
        }

        private Maidata _lastSelectedMaidata;
        private void OnLevelSelected(object sender, ListEventArgs e)
        {
            if (sender is not List list)
                return;

            var selectedItem = list.ItemObjectList[e.Index];

            if (selectedItem is not LevelListItem levelListItem)
                return;

            if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Alphabet)
            {
                while (levelListItem.maidata.Difficulties[_selectedDifficulty] == string.Empty &&
                       levelListItem.maidata.Designers[_selectedDifficulty] == string.Empty &&
                       levelListItem.maidata.Charts[_selectedDifficulty] == string.Empty)
                {
                    if (_selectedDifficulty < 1)
                        _selectedDifficulty = 6;
                    _selectedDifficulty -= 1;
                }

                levelListItem.difficultyIndex = _selectedDifficulty;
            }

            backgroundImage.color =
                backgroundColors[levelListItem.maidata.IsUtage ? 5 : levelListItem.difficultyIndex];

            difficultyText.text = levelListItem.maidata.Difficulties[levelListItem.difficultyIndex];

            var designerName = levelListItem.maidata.Designers[levelListItem.difficultyIndex];

            designerName = designerName == "\r" ? levelListItem.maidata.MainChartDesigner : designerName;
            designerName = designerName == "\r" ? "Unknown Designer" : designerName;

            charterNameText.text = designerName;

            difficultyNameText.text = (levelListItem.maidata.IsUtage ? 6 : levelListItem.difficultyIndex) switch
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

            var textColor = textColors[levelListItem.maidata.IsUtage ? 5 : levelListItem.difficultyIndex];

            difficultyText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyText.color.a);
            charterNameText.color = new Color(textColor.r, textColor.g, textColor.b, charterNameText.color.a);
            difficultyNameText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyNameText.color.a);
            difficultyNameText.colorGradient =
                textGradientColors[levelListItem.maidata.IsUtage ? 5 : levelListItem.difficultyIndex];

            AddMotionHandle(LMotion.Create(0, 1f, e.IndexChangeIsAnimated ? 0.5f : 0).WithEase(Ease.OutExpo).Bind(x =>
            {
                canvasGroup.alpha = x;
                mask.position = new Vector3(mask.position.x,
                    LevelListController.GetInstance().levelList.ItemObjectList[e.Index]
                        .transform.position.y, mask.position.z);
            }));

            if (_chartLoadingRoutine != null)
            {
                StopCoroutine(_chartLoadingRoutine);
                _chartLoadingRoutine = null;
            }

            _chartLoadingRoutine = StartCoroutine(LoadChartResource(levelListItem.maidata));
        }

        private Coroutine _chartLoadingRoutine;

        private IEnumerator LoadChartResource(Maidata maidata)
        {
            if (maidata == _lastSelectedMaidata)
                yield break;

            yield return new WaitForSeconds(1f);

            yield return maidata.LoadSong();

            yield return maidata.GenerateBlurredCover();

            if (_lastSelectedMaidata != null)
            {
                Destroy(_lastSelectedMaidata.SongAudioClip);
                Destroy(_lastSelectedMaidata.SongCoverBlurred);
                Destroy(_lastSelectedMaidata.SongCoverBlurredAsBackground);
                _lastSelectedMaidata.SongLoaded = false;
                _lastSelectedMaidata.BlurredSongCoverGenerated = false;
            }

            _lastSelectedMaidata = maidata;
        }
    }
}