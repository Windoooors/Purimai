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

            SimulatedSensor.OnTap += (_, args) =>
            {
                if (args.SensorId == "A2")
                    ChangeDifficulty(-1);
                if (args.SensorId == "A3")
                    ChangeDifficulty(1);
                if (args.SensorId == "A5")
                    EnterLevel();
            };

            Instance = this;
        }

        private void EnterLevel()
        {
            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            var levelListController = LevelListController.GetInstance();

            var difficultyIndex =
                ((LevelListItem)levelListController.levelList.ItemObjectList[levelListController.levelList.index])
                .difficultyIndex;

            var maidata =
                ((LevelListItem)levelListController.levelList.ItemObjectList[levelListController.levelList.index])
                .chartData;

            StartCoroutine(LoadChart(LoadScene, maidata.SongPath, maidata, difficultyIndex));
        }

        private void LoadScene(AudioClip audioClip, LevelListController.Maidata maidata, int difficultyIndex)
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

                chartPlayer.backgroundImage.transform.localScale *=
                    SettingsPool.GetValue("game.blurred_cover") == 1
                        ? 1.1f
                        : 1;

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

            UIManager.Instance.canvas.enabled = false;

            yield return new WaitForSeconds(1);

            chartPlayer.Play();
        }

        private IEnumerator LoadChart(Action<AudioClip, LevelListController.Maidata, int> onComplete, string path,
            LevelListController.Maidata maidata, int chartIndex)
        {
            var audioType = Path.GetExtension(path).ToLower() switch
            {
                ".ogg" => AudioType.OGGVORBIS,
                ".wav" => AudioType.WAV,
                ".mp2" or ".mp3" => AudioType.MPEG,
                _ => AudioType.UNKNOWN
            };

            if (audioType == AudioType.UNKNOWN)
                throw new Exception("Unknown audio type: " + path);

            var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path), audioType);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception("Failed to load audio: " + request.error);

            var clip = DownloadHandlerAudioClip.GetContent(request);
            request.Dispose();

            yield return maidata.GenerateBlurredCover();

            onComplete(clip, maidata, chartIndex);
            yield return null;
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

                    if (levelListItem.chartData == currentLevelListItem.chartData &&
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

                if (currentLevelListItem.chartData.Difficulties[_selectedDifficulty] == string.Empty &&
                    currentLevelListItem.chartData.Designers[_selectedDifficulty] == string.Empty &&
                    currentLevelListItem.chartData.Charts[_selectedDifficulty] == string.Empty)
                    _selectedDifficulty -= direction;

                currentLevelListItem.difficultyIndex = _selectedDifficulty;

                if (lastSelectedDifficulty != _selectedDifficulty)
                    OnLevelSelected(LevelListController.GetInstance().levelList,
                        new ListEventArgs(LevelListController.GetInstance().levelList.index, true));
            }
        }

        private void OnLevelSelected(object sender, ListEventArgs e)
        {
            if (sender is not List list)
                return;

            var selectedItem = list.ItemObjectList[e.Index];

            if (selectedItem is not LevelListItem levelListItem)
                return;

            if (LevelListController.GetInstance().groupByRule == LevelListController.SortingRules.Alphabet)
            {
                while (levelListItem.chartData.Difficulties[_selectedDifficulty] == string.Empty &&
                       levelListItem.chartData.Designers[_selectedDifficulty] == string.Empty &&
                       levelListItem.chartData.Charts[_selectedDifficulty] == string.Empty)
                {
                    if (_selectedDifficulty < 1)
                        _selectedDifficulty = 6;
                    _selectedDifficulty -= 1;
                }

                levelListItem.difficultyIndex = _selectedDifficulty;
            }

            backgroundImage.color = backgroundColors[levelListItem.difficultyIndex];

            difficultyText.text = levelListItem.chartData.Difficulties[levelListItem.difficultyIndex];

            var designerName = levelListItem.chartData.Designers[levelListItem.difficultyIndex];

            designerName = designerName == "\r" ? levelListItem.chartData.MainChartDesigner : designerName;
            designerName = designerName == "\r" ? "Unknown Designer" : designerName;

            charterNameText.text = designerName;

            difficultyNameText.text = levelListItem.difficultyIndex switch
            {
                0 => "EZ",
                1 => "BAS",
                2 => "ADV",
                3 => "EXP",
                4 => "MAS",
                5 => "RE",
                _ => ""
            };

            var textColor = textColors[levelListItem.difficultyIndex];

            difficultyText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyText.color.a);
            charterNameText.color = new Color(textColor.r, textColor.g, textColor.b, charterNameText.color.a);
            difficultyNameText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyNameText.color.a);
            difficultyNameText.colorGradient = textGradientColors[levelListItem.difficultyIndex];

            AddMotionHandle(LMotion.Create(0, 1f, e.IndexChangeIsAnimated ? 0.5f : 0).WithEase(Ease.OutExpo).Bind(x =>
            {
                canvasGroup.alpha = x;
                mask.position = new Vector3(mask.position.x,
                    LevelListController.GetInstance().levelList.ItemObjectList[e.Index]
                        .transform.position.y, mask.position.z);
            }));
        }
    }
}