using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ChartManagement;
using Game;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.LevelSelection
{
    public class DifficultyIndicator : MonoBehaviour
    {
        public Sprite[] backgroundSprites;
        public Color[] textColors;

        public Image backgroundImage;

        public TextMeshProUGUI difficultyText;

        public TextMeshProUGUI difficultyNameText;

        public CanvasGroup canvasGroup;

        public int difficultyIndexWhenUseAlphabetGroups;

        private void Start()
        {
            LevelListController.Instance.levelList.OnItemSelected += OnLevelSelected;

            SimulatedSensor.OnTap += (sender, args) =>
            {
                if (args.SensorId == "A2")
                    ChangeDifficulty(-1);
                if (args.SensorId == "A3")
                    ChangeDifficulty(1);
                if (args.SensorId == "A5")
                    EnterLevel();
            };
        }

        private void EnterLevel()
        {
            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;
            
            var levelListController = LevelListController.Instance;

            var difficultyIndex = levelListController.groupByRule == LevelListController.SortingRules.Difficulty
                ? ((LevelListItem)levelListController.levelList.ItemObjectList[levelListController.levelList.index])
                .difficultyIndex
                : difficultyIndexWhenUseAlphabetGroups;

            var maidata =
                ((LevelListItem)levelListController.levelList.ItemObjectList[levelListController.levelList.index])
                .chartData;

            var firstNoteTime = maidata.FirstNoteTime;
            var chart = maidata.Charts[difficultyIndex];

            StartCoroutine(LoadChart(LoadScene, maidata.SongPath, firstNoteTime, chart));
        }

        private void LoadScene(AudioClip audioClip, float firstNoteTime, string chart)
        {
            SceneManager.LoadScene("Game");
            
            SceneManager.sceneLoaded += (_, _) =>
            {
                var noteGenerator =
                    FindAnyObjectByType<NoteGenerator>(FindObjectsInactive.Include);
                noteGenerator.GenerateNotes(chart, firstNoteTime );

                var chartPlayer = FindAnyObjectByType<ChartPlayer>(FindObjectsInactive.Include);
                chartPlayer.audioSource.clip = audioClip;

                StartCoroutine(PlayChart(chartPlayer));
            };
        }

        private IEnumerator PlayChart(ChartPlayer chartPlayer)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForEndOfFrame();
            }
            
            UIManager.Instance.canvas.enabled = false;
            
            yield return new WaitForSeconds(3);
            
            chartPlayer.Play();
        }

        private IEnumerator LoadChart(Action<AudioClip, float, string> onComplete, string path, float firstNoteTime, string chart)
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
            request.SendWebRequest();

            AudioClip clip;

            while (true)
            {
                if (!request.isDone)
                    continue;
                clip = DownloadHandlerAudioClip.GetContent(request);
                request.Dispose();
                
                break;
            }
            
            onComplete(clip, firstNoteTime, chart);
            yield return null;
        }
        
        private void ChangeDifficulty(int direction)
        {
            if (LevelListController.Instance.groupByRule != LevelListController.SortingRules.Difficulty)
                return;

            var list = LevelListController.Instance.levelList;

            if (list.ItemObjectList[list.index] is not LevelListItem currentLevelListItem)
                return;

            var currentDifficultyIndex = currentLevelListItem.difficultyIndex;

            var targetIndex = list.index;
            for (int i = 0; i < list.ItemObjectList.Count; i++)
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

        private void OnLevelSelected(object sender, ListEventArgs e)
        {
            if (sender is not List list)
                return;

            var selectedItem = list.ItemObjectList[e.Index];

            if (selectedItem is not LevelListItem levelListItem)
                return;

            LMotion.Create(1f, 0, e.IndexChangeIsAnimated ? 0.1f : 0).WithOnComplete(() =>
            {
                backgroundImage.sprite = backgroundSprites[levelListItem.difficultyIndex];

                difficultyText.text = levelListItem.chartData.DifficultyNames[levelListItem.difficultyIndex];

                difficultyNameText.text = levelListItem.difficultyIndex switch
                {
                    0 => "Ez",
                    1 => "Bas",
                    2 => "Adv",
                    3 => "Exp",
                    4 => "Mas",
                    5 => "Re",
                    _ => ""
                };

                var textColor = textColors[levelListItem.difficultyIndex];

                difficultyText.color = textColor;
                difficultyNameText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyNameText.color.a);

                LMotion.Create(0, 1f, e.IndexChangeIsAnimated ? 0.1f : 0).Bind(x => canvasGroup.alpha = x);
            }).Bind(x => canvasGroup.alpha = x);
        }
    }
}