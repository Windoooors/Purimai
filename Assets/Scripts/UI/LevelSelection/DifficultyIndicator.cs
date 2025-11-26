using System.Collections;
using System.Threading.Tasks;
using FMOD;
using Game;
using LitMotion;
using TMPro;
using UI.GameSettings;
using UI.Result;
using UnityEngine;
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

        private Maidata _lastSelectedMaidata;

        private int _selectedDifficulty;

        private void OnEnable()
        {
            LevelListController.GetInstance().levelList.OnItemSelected += OnLevelSelected;

            SimulatedSensor.OnTap += OnTap;

            Instance = this;
        }

        private void OnDisable()
        {
            SimulatedSensor.OnTap -= OnTap;
            LevelListController.GetInstance().levelList.OnItemSelected -= OnLevelSelected;
        }

        private void OnTap(object _, TouchEventArgs args)
        {
            if (args.SensorId == "A2")
                ChangeDifficulty(-1);
            if (args.SensorId == "A3")
                ChangeDifficulty(1);
            if (args.SensorId == "A5")
                StartCoroutine(EnterLevel());
        }

        private IEnumerator EnterLevel()
        {
            var levelListController = LevelListController.GetInstance();

            var difficultyIndex =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .DifficultyIndex;

            var maidata =
                ((LevelListItemData)levelListController.levelList.AllData[levelListController.levelList.dataIndex])
                .Maidata;

            if (!maidata.SongLoaded || !maidata.BlurredSongCoverGenerated)
                Task.Run(() =>
                {
                    maidata.GenerateBlurredCover();
                    maidata.LoadSongClip();
                });

            while (true)
            {
                yield return null;

                if (!maidata.SongLoaded || maidata.BlurredSongCoverAsBackgroundDecodedImage == null)
                    continue;

                break;
            }

            SimulatedSensor.OnTap = null;
            SimulatedSensor.OnHold = null;
            SimulatedSensor.OnLeave = null;

            var originalListPosition = levelListController.levelList.transform.position;
            var originalPosition = transform.position;

            var currentAlpha = levelListController.songCoverBackgroundImage.color.a;

            levelListController.songCoverBackgroundImage.sprite = SettingsPool.GetValue("game.blurred_cover") == 1
                ? maidata.BlurredSongCoverAsBackgroundDecodedImage.GetSprite()
                : maidata.SongCoverDecodedImage.GetSprite();

            levelListController.songCoverBackgroundImage.transform.localScale =
                SettingsPool.GetValue("game.blurred_cover") == 1 ? Vector3.one * 1.1f : Vector3.one;

            AddMotionHandle(LMotion.Create(
                    0, 15f, 0.5f).WithEase(Ease.InExpo).WithOnComplete(() =>
                {
                    LoadScene(maidata.SongFMODSound, maidata, difficultyIndex);
                    levelListController.backgroundImage.enabled = false;
                })
                .Bind(x =>
                {
                    levelListController.levelList.transform.position = originalListPosition + new Vector3(-x, 0, 0);
                    var rgbValue = (1 - x / 15f) * 0.43529412f + 0.56470588f;
                    levelListController.songCoverBackgroundImage.color =
                        new Color(rgbValue, rgbValue, rgbValue, x / 15f * (1 - currentAlpha) + currentAlpha);
                    transform.position = originalPosition + new Vector3(x * 0.3f, 0, 0);
                })
            );
        }

        private void LoadScene(Sound sound, Maidata maidata, int difficultyIndex)
        {
            SceneManager.LoadScene("Game");

            SceneManager.sceneLoaded += (_, _) =>
            {
                var noteGenerator =
                    FindAnyObjectByType<NoteGenerator>(FindObjectsInactive.Include);
                noteGenerator.GenerateNotes(maidata.Charts[difficultyIndex], maidata.FirstNoteTime);

                var chartPlayer = FindAnyObjectByType<ChartPlayer>(FindObjectsInactive.Include);

                chartPlayer.SongClip = sound;

                chartPlayer.backgroundImage.sprite = SettingsPool.GetValue("game.blurred_cover") == 1
                    ? maidata.BlurredSongCoverAsBackgroundDecodedImage.GetSprite()
                    : maidata.SongCoverDecodedImage.GetSprite();

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

            AddMotionHandle(LMotion.Create(1, 0f, 0.5f).WithEase(Ease.OutExpo).WithOnComplete(() =>
                {
                    LevelListController.GetInstance().levelList.transform.position += new Vector3(15, 0, 0);
                    transform.position += new Vector3(-5, 0, 0);
                    StartCoroutine(WaitAndPlay());
                    LevelListController.GetInstance().backgroundImage.enabled = true;
                })
                .Bind(x => { UIManager.GetInstance().maskCanvasGroup.alpha = x; }));

            yield break;

            IEnumerator WaitAndPlay()
            {
                yield return new WaitForSeconds(1);

                chartPlayer.Play();
                UIManager.GetInstance().DisableUI();
            }
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

            var textColor = textColors[levelListItemData.Maidata.IsUtage ? 5 : levelListItemData.DifficultyIndex];

            difficultyText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyText.color.a);
            charterNameText.color = new Color(textColor.r, textColor.g, textColor.b, charterNameText.color.a);
            difficultyNameText.color = new Color(textColor.r, textColor.g, textColor.b, difficultyNameText.color.a);
            difficultyNameText.colorGradient =
                textGradientColors[levelListItemData.Maidata.IsUtage ? 5 : levelListItemData.DifficultyIndex];

            if (e.IndexChangeIsAnimated)
            {
                AddMotionHandle(LMotion.Create(0, 1f, 0.5f).WithEase(Ease.OutExpo).Bind(x =>
                {
                    canvasGroup.alpha = x;
                    transform.position = new Vector3(transform.position.x,
                        LevelListController.GetInstance().levelList.GetSelectedItemObject().transform.position.y,
                        transform.position.z);
                }));
            }
            else
            {
                canvasGroup.alpha = 1;
                transform.position = new Vector3(transform.position.x,
                    LevelListController.GetInstance().levelList.GetSelectedItemObject().transform.position.y,
                    transform.position.z);
            }
        }
    }
}