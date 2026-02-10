using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game;
using GihanSoft.String;
using TinyPinyin;
using UI.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.LevelSelection
{
    public class LevelSelectionManager : MonoBehaviour
    {
        public enum SortingRules
        {
            Alphabet,
            Difficulty
        }

        private const float ItemHeight = 126;
        private const int VirtualCount = 100000;

        private static LevelSelectionManager _instance;

        public SortingRules groupByRule;

        public VisualTreeAsset itemTemplate;

        public VisualTreeAsset levelSelectionTreeAsset;

        public LevelLoader levelLoader;

        private static readonly List<MaidataReferenceCountPair> _maidataList = new();

        private LevelListItemData[] _data;
        private VisualElement _largeSongCover;

        private CategoryData _lastCategoryData;

        public VisualElement LevelSelectionTree;

        private ListView _listView;
        private LevelListItemData[] _rawData;
        private ScoreContentPanel _scoreContentPanel;
        private ScrollView _scrollView;
        private Button _settingsButton;

        private SnapScrollManipulator _snapManipulator;
        private SongCoverManipulator _songCoverManipulator;

        private Button _sortButton;

        private void Awake()
        {
            LevelSelectionTree = levelSelectionTreeAsset.Instantiate();

            LevelSelectionTree.style.position = Position.Absolute;
            LevelSelectionTree.style.left = 0;
            LevelSelectionTree.style.top = 0;
            LevelSelectionTree.style.bottom = 0;
            LevelSelectionTree.style.right = 0;

            UIManager.GetInstance().uiDocument.rootVisualElement.Add(LevelSelectionTree);

            _instance = this;
            
            groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            if (_maidataList.Count == 0)
            {
                var path = Path.Combine(Application.persistentDataPath, "Charts/");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                foreach (var levelPath in Directory.GetDirectories(path))
                {
                    if (!(FileExistsIgnoreCase(Path.Combine(levelPath, "maidata.txt"), out var actualMaidataPath) &&
                          (FileExistsIgnoreCase(Path.Combine(levelPath, "track.mp3"), out var actualSongMp3Path) ||
                           FileExistsIgnoreCase(Path.Combine(levelPath, "track.ogg"), out var actualSongOggPath))))
                        continue;

                    actualSongOggPath = "";

                    var aviExists = FileExistsIgnoreCase(Path.Combine(levelPath, "pv.avi"), out var actualPvPathAvi);
                    FileExistsIgnoreCase(Path.Combine(levelPath, "pv.mp4"), out var actualPvPathMp4);

                    var pngExists = FileExistsIgnoreCase(Path.Combine(levelPath, "bg.png"), out var actualBgPathPng);
                    var jpgExists = FileExistsIgnoreCase(Path.Combine(levelPath, "bg.jpg"), out var actualBgPathJpg);

                    if (!jpgExists)
                        FileExistsIgnoreCase(Path.Combine(levelPath, "bg.jpeg"), out actualBgPathJpg);

                    var maidata = new Maidata(actualMaidataPath,
                        File.Exists(actualSongMp3Path) ? actualSongMp3Path : actualSongOggPath,
                        aviExists ? actualPvPathAvi : actualPvPathMp4,
                        pngExists ? actualBgPathPng : actualBgPathJpg);

                    _maidataList.Add(new MaidataReferenceCountPair
                    {
                        Maidata = maidata,
                        Referenced = false
                    });
                }

                UIManager.GetInstance().UpdateTMPAtlas(Maidata.UsedCharacters.ToArray());
            }

            levelLoader.PlayerPrefsSavingProcedure += () =>
            {
                var index = _listView.selectedIndex % _rawData.Length;

                PlayerPrefs.SetInt("LevelListIndex", index);
            };

            Initialize();
        }

        private void Update()
        {
            SetStyle();

            if (_listView.selectedIndex >= _data.Length || _listView.selectedIndex < 0)
                return;

            var maidata = _data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata;

            if (maidata.CoverDataLoaded && _largeSongCover != null)
                _largeSongCover.style.backgroundImage = maidata.SongCoverDecodedImage.GetTexture2D();

            foreach (var pair in _maidataList)
                if (!pair.Referenced && pair.Maidata.CoverDataLoaded)
                    pair.Maidata.UnloadSongCover();
            
            if (maidata.SongLoaded && !_songPlaying)
            {
                _songPlaying = true;

                if (_songPreviewAudioSourceHandler == null)
                    AudioManager.GetInstance().AudioSourcePool
                        .TryGetAudioSourceHandler(out _songPreviewAudioSourceHandler);
                
                _songPreviewAudioSourceHandler.SetClip(maidata.SongAudioClip);
                
                _songPreviewAudioSourceHandler.Play();
                
                var volume = SettingsPool.GetValue("audio.volume.song") / 10f;
                
                _songPreviewAudioSourceHandler.SetVolume(volume);
            }

            if (_songPlaying && _songPreviewAudioSourceHandler?.IsPlaying() == false)
            {
                _songPreviewAudioSourceHandler.Play();
            }
        }

        private AudioSourcePool.AudioSourceHandler _songPreviewAudioSourceHandler;

        private void OnDestroy()
        {
            SettingsManager.OnSettingsChanged -= InitializeGroupingRule;
            SettingsManager.OnSettingsChanged -= ChangeVolume;

            _scrollView.RemoveManipulator(_snapManipulator);
            _largeSongCover.RemoveManipulator(_songCoverManipulator);
            
            UIManager.GetInstance().uiDocument?.rootVisualElement?.Remove(LevelSelectionTree);
        }

        private void ChangeVolume()
        {
            var volume = SettingsPool.GetValue("audio.volume.song") / 10f;
                
            _songPreviewAudioSourceHandler?.SetVolume(volume);
        }

        private void OnApplicationQuit()
        {
            var index = _listView.selectedIndex % _rawData.Length;

            PlayerPrefs.SetInt("LevelListIndex", index);
        }

        private static bool FileExistsIgnoreCase(string input, out string actualPath)
        {
            actualPath = "";

            if (string.IsNullOrEmpty(input)) return false;

            try
            {
                string directory = Path.GetDirectoryName(input);
                string fileName = Path.GetFileName(input);

                if (string.IsNullOrEmpty(directory)) directory = Directory.GetCurrentDirectory();

                if (!Directory.Exists(directory)) return false;

                string[] matches = Directory.GetFiles(directory, fileName, new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = false
                });

                if (matches.Length > 0)
                {
                    actualPath = matches[0];
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private void ChangeDifficultyInNumericalView(object sender, ScoreContentPanel.DifficultyChangeEventArgs e)
        {
            if (groupByRule != SortingRules.Difficulty)
                return;

            var currentMaidata = _data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata;

            var targetChart = currentMaidata.Charts[e.TargetChartIndex];

            var targetDifficultyIndex = targetChart.DifficultyIndex;

            var rawDataList = _rawData.ToList();

            var targetItem = rawDataList.Find(x =>
                x.DifficultyIndex == targetDifficultyIndex && x.MaidataReferenceCountPair.Maidata == currentMaidata);

            var targetItemIndex = rawDataList.IndexOf(targetItem);

            _snapManipulator.SnapToNearest(e.Direction, targetItemIndex, _listView.selectedIndex, rawDataList.Count,
                _scrollView, out _, true, false);
        }

        private void ChangeCategory(object sender, CategoryListManager.ChangeCategoryEventArgs e)
        {
            var targetItemIndex = _rawData.ToList().IndexOf(e.TargetItem);

            _snapManipulator.SnapToNearest(e.Direction, targetItemIndex, _listView.selectedIndex, _rawData.Length,
                _scrollView, out _, false, false);
        }

        private void Initialize()
        {
            /*Initialize List*/

            var root = LevelSelectionTree;
            _listView = root.Q<VisualElement>("list-parent").Q<ListView>("list");

            _scrollView = _listView.Q<ScrollView>();

            _scoreContentPanel = root.Q<ScoreContentPanel>();

            _scoreContentPanel.OnDifficultyTendsToChange += ChangeDifficultyInNumericalView;

            _listView.fixedItemHeight = ItemHeight;

            _listView.makeItem = () =>
            {
                var element = itemTemplate.Instantiate();
                return element;
            };

            _listView.bindItem = (element, index) =>
            {
                var informationElement = element.Q<VisualElement>("song-item").Q<VisualElement>("information");

                var songTitleWaterMark = element.Q<VisualElement>("song-item").Q<Label>("song-title-watermark");
                var songTitleLabel = informationElement.Q<Label>("song-title");

                var maidata = _data[index].MaidataReferenceCountPair;

                songTitleLabel.text = maidata.Maidata.Title;

                songTitleWaterMark.text =
                    $"<color=white><gradient=\"level-item-watermark\">{maidata.Maidata.Title}</gradient></color>";

                var artistLabel = informationElement.Q<Label>("song-artist");
                var bpmLabel = informationElement.Q<Label>("song-bpm");

                artistLabel.text = maidata.Maidata.Artist;
                bpmLabel.text = maidata.Maidata.Bpm.ToString("");

                if (!maidata.Maidata.CoverDataLoaded) Task.Run(() => { maidata.Maidata.LoadSongCover(); });

                element.userData = index;

                SetSingleItemStyle(element);
                element.style.visibility = Visibility.Visible;

                element.RemoveFromClassList("enable-transition");

                element.RemoveFromClassList("show-cover");

                element.Q<Button>().clicked -= OnClick;
                element.Q<Button>().clicked += OnClick;

                return;

                void OnClick()
                {
                    OnItemClicked(element);
                }
            };

            _listView.unbindItem = (element, index) =>
            {
                _data[index].MaidataReferenceCountPair.Referenced = false;

                element.RemoveFromClassList("enable-transition");

                element.RemoveFromClassList("show-cover");

                element.style.visibility = Visibility.Hidden;
            };

            CategoryListManager.GetInstance.Initialize();
            CategoryListManager.GetInstance.OnCategoryTendsToChange += ChangeCategory;

            _largeSongCover = root.Q<VisualElement>("song-cover").Q<VisualElement>("song-cover-image");

            _songCoverManipulator = new SongCoverManipulator(SongCoverManipulator.SongCoverLayoutPopulationMode.FixedHeight, 0);

            _largeSongCover.AddManipulator(_songCoverManipulator);

            _sortButton = root.Q<VisualElement>("control-panel").Q<Button>("sort-button");
            _sortButton.clicked += ChangeGroupingRule;
            SettingsManager.OnSettingsChanged += InitializeGroupingRule;
            SettingsManager.OnSettingsChanged += ChangeVolume;

            _settingsButton = root.Q<VisualElement>("control-panel").Q<Button>("settings-button");
            _settingsButton.clicked += UIManager.GetInstance().ShowSettingsPanel;

            _snapManipulator = new SnapScrollManipulator(ItemHeight, 741);
            _scrollView.AddManipulator(_snapManipulator);
            _scrollView.verticalScroller.valueChanged += _ => { SetStyle(); };

            InitializeGroupingRule();
            
            LoadSong(_listView.selectedIndex);

            _listView.selectionChanged += _ =>
            {
                _listView.Query<TemplateContainer>().ForEach(item =>
                    {
                        if (!item.ClassListContains("enable-transition")) item.AddToClassList("enable-transition");
                    }
                );

                _scoreContentPanel.SetChartInformationData(
                    _data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata,
                    groupByRule == SortingRules.Difficulty
                        ? _data[_listView.selectedIndex].DifficultyIndex
                        : _scoreContentPanel.AlphabeticallySelectedDifficultyIndex);
                _scoreContentPanel.SetScoreData(
                    ChartRankDataManager.GetChartRankData(_data[_listView.selectedIndex].MaidataReferenceCountPair
                        .Maidata.MaidataDirectoryName), groupByRule == SortingRules.Difficulty
                        ? _data[_listView.selectedIndex].DifficultyIndex
                        : _scoreContentPanel.AlphabeticallySelectedDifficultyIndex);
                
                //AudioManager.GetInstance().PlaySelectSound();
            };

            _snapManipulator.OnSnapToItem += (_, args) =>
            {
                LoadSong(args.TargetIndex);

                if (!args.IsByHand)
                    return;

                if (_lastCategoryData != _data[args.TargetIndex].Category)
                    CategoryListManager.GetInstance.ChangeCategoryPassively(_data[args.TargetIndex].Category);

                _lastCategoryData = _data[args.TargetIndex].Category;
            };
        }

        private void LoadSong(int index)
        {
            if (_lastPreviewedMaidata != null && _lastPreviewedMaidata ==
                _data[index].MaidataReferenceCountPair.Maidata)
                return;
            
            _lastPreviewedMaidata?.UnloadSong();
                
            StartCoroutine(_data[index].MaidataReferenceCountPair.Maidata.LoadSongClip());
            _songPlaying = false;

            _lastPreviewedMaidata = _data[index].MaidataReferenceCountPair.Maidata;
        }

        private Maidata _lastPreviewedMaidata;

        private bool _songPlaying;

        private void InitializeGroupingRule()
        {
            groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            var currentData = _data?[_listView.selectedIndex];

            var pairedData = GetLevelListItemData(groupByRule);

            _rawData = pairedData.Item1;

            var dataList = new List<LevelListItemData>();

            while (dataList.Count < VirtualCount) dataList.AddRange(_rawData);

            _data = dataList.ToArray();

            _listView.itemsSource = _data;

            var targetRawIndex = 0;

            if (currentData != null)
                switch (groupByRule)
                {
                    case SortingRules.Alphabet:
                    {
                        for (var i = 0; i < _rawData.Length; i++)
                        {
                            var maidata = _rawData[i];
                            if (maidata.MaidataReferenceCountPair.Maidata.Title ==
                                currentData.MaidataReferenceCountPair.Maidata.Title)
                            {
                                targetRawIndex = i;
                                break;
                            }
                        }

                        break;
                    }
                    case SortingRules.Difficulty:
                    {
                        for (var i = 0; i < _rawData.Length; i++)
                        {
                            var maidata = _rawData[i];
                            if (maidata.MaidataReferenceCountPair.Maidata.Title ==
                                currentData.MaidataReferenceCountPair.Maidata.Title && maidata.DifficultyIndex ==
                                _scoreContentPanel.AlphabeticallySelectedDifficultyIndex)
                            {
                                targetRawIndex = i;
                                break;
                            }
                        }

                        break;
                    }
                }

            if (currentData == null) targetRawIndex = PlayerPrefs.GetInt("LevelListIndex");

            _snapManipulator.SnapToNearest(1, targetRawIndex, VirtualCount / 2, _rawData.Length, _scrollView,
                out var targetIndex, false, false);

            _sortButton.RemoveFromClassList("sort-button-difficulty");
            _sortButton.RemoveFromClassList("sort-button-alphabetically");

            _sortButton.AddToClassList(groupByRule is SortingRules.Alphabet
                ? "sort-button-alphabetically"
                : "sort-button-difficulty");

            CategoryListManager.GetInstance.ChangeData(pairedData.Item2);

            _listView.selectedIndex = targetIndex;

            _scoreContentPanel.SetChartInformationData(
                _data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata,
                groupByRule == SortingRules.Difficulty
                    ? _data[_listView.selectedIndex].DifficultyIndex
                    : _scoreContentPanel.AlphabeticallySelectedDifficultyIndex);
            _scoreContentPanel.SetScoreData(
                ChartRankDataManager.GetChartRankData(_data[_listView.selectedIndex].MaidataReferenceCountPair
                    .Maidata.MaidataDirectoryName), groupByRule == SortingRules.Difficulty
                    ? _data[_listView.selectedIndex].DifficultyIndex
                    : _scoreContentPanel.AlphabeticallySelectedDifficultyIndex);

            if (_lastCategoryData != _data[targetIndex].Category)
                CategoryListManager.GetInstance.ChangeCategoryPassively(_data[targetIndex].Category);

            _lastCategoryData = _data[targetIndex].Category;
        }

        private void ChangeGroupingRule()
        {
            groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            SettingsPool.SetValue("song_list.group_rule", groupByRule is SortingRules.Alphabet ? 1 : 0);

            SettingsPool.Save();

            InitializeGroupingRule();
        }

        private void OnItemClicked(VisualElement element)
        {
            var index = (int)(element?.userData ?? 0);

            if (_listView.selectedIndex != index)
            {
                _snapManipulator.SnapToItem(index, _scrollView, true, true);
                return;
            }
            
            LevelSelectionTree.AddToClassList("confirm-entry");

            _scrollView.SetEnabled(false);

            var confirmEntryBackground = LevelSelectionTree.Q<VisualElement>("confirm-entry-background");

            _listView.RegisterCallback<PointerUpEvent>(Registered);

            confirmEntryBackground.RegisterCallback<PointerUpEvent>(Registered);

            //Invoke(nameof(EnterLevel), 0.5f);

            LevelSelectionTree.Q<Button>("play-button").clicked += () =>
            {
                LevelSelectionTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/LevelSelection/LevelSelectionToGameInAnimated"));
                StartCoroutine(EnterLevel());
            };

            return;

            IEnumerator EnterLevel()
            {
                yield return new WaitForSeconds(0.5f);
                
                LevelLoader.GetInstance.SceneLoaded += () =>
                {
                    LevelLoader.GetInstance.SceneLoaded = null;

                    StartCoroutine(PlayWhiteOutAnimation());
                };
            
                CategoryListManager.GetInstance.enabled = false;
                _listView.unbindItem = null;

                _snapManipulator.OnSnapToItem = null;
            
                LevelLoader.GetInstance.EnterLevel(_data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata,
                    groupByRule == SortingRules.Difficulty
                        ? _data[_listView.selectedIndex].DifficultyIndex
                        : _scoreContentPanel.AlphabeticallySelectedDifficultyIndex);

            }

            IEnumerator PlayWhiteOutAnimation()
            {
                yield return new WaitForSeconds(0.1f);

                LevelSelectionTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/LevelSelection/LevelSelectionToGameOutAnimated"));

                yield return new WaitForSeconds(0.5f);
                
                RemoveThis();
            }

            void Registered(PointerUpEvent evt)
            {
                PointerUp();
                _listView.UnregisterCallback<PointerUpEvent>(Registered);
                confirmEntryBackground.UnregisterCallback<PointerUpEvent>(Registered);
            }

            void PointerUp()
            {
                LevelSelectionTree.RemoveFromClassList("confirm-entry");
                _scrollView.SetEnabled(true);
            }
        }

        private void RemoveThis()
        {
            Destroy(gameObject);
        }
        
        private void SetStyle()
        {
            _listView.Query<TemplateContainer>().ForEach(SetSingleItemStyle);
        }

        private void SetSingleItemStyle(VisualElement item)
        {
            var relativeY = item.layout.y - _scrollView.verticalScroller.value;

            var normalized = relativeY / 1500 - 0.5f;

            item.style.left = normalized * normalized * 1050 - 100;

            //var centerDist = Mathf.Abs(relativeY - 250);
            //item.style.opacity = Mathf.Clamp01(1 - centerDist / 250 * 0.5f);

            var index = (int)item.userData;

            _data[index].MaidataReferenceCountPair.Referenced = true;

            var maidata = _data[index].MaidataReferenceCountPair.Maidata;

            if (maidata.CoverDataLoaded)
            {
                var songCoverElement = item.Q<VisualElement>("song-item").Q<VisualElement>("song-cover");
                var tex = maidata.SongCoverDecodedImage.GetTexture2D();
                songCoverElement.style.backgroundImage = new StyleBackground(tex);

                if (!item.ClassListContains("show-cover"))
                    item.AddToClassList("show-cover");
            }

            if (relativeY is < 750 and > 730) _listView.selectedIndex = (int)item.userData;
        }

        public static LevelSelectionManager GetInstance()
        {
            return _instance ? _instance : FindAnyObjectByType<LevelSelectionManager>();
        }

        private (LevelListItemData[], CategoryData[]) GetLevelListItemData(SortingRules rule)
        {
            var groups = new List<((MaidataReferenceCountPair, int)[], string)>();

            switch (rule)
            {
                case SortingRules.Difficulty:
                    var difficultyStringHashSet = new HashSet<string>();

                    foreach (var maidata in _maidataList)
                    foreach (var chart in maidata.Maidata.Charts)
                    {
                        var difficultyName = chart.DifficultyString;

                        difficultyStringHashSet.Add(difficultyName);
                    }

                    var sortedCustomizedDifficultyNames = difficultyStringHashSet.ToList();
                    sortedCustomizedDifficultyNames.Sort((x, y) =>
                        new NaturalComparer().Compare(x, y));

                    AddGroupByDifficulty(sortedCustomizedDifficultyNames.ToArray());

                    break;
                default:
                case SortingRules.Alphabet:
                    var groupNames = new HashSet<string>();

                    foreach (var maidata in _maidataList)
                    {
                        var firstCharacterIsLetterOrDigit = char.IsLetterOrDigit(maidata.Maidata.Title[0]);
                        var firstCharacterIsInChinese =
                            PinyinHelper.IsChinese(maidata.Maidata.Title[0]);
                        var pinyinOfFirstCharacter =
                            PinyinHelper.GetPinyin(maidata.Maidata.Title[0]);

                        if (firstCharacterIsInChinese)
                        {
                            groupNames.Add(pinyinOfFirstCharacter.ToUpper()[0].ToString());
                        }
                        else if (firstCharacterIsLetterOrDigit)
                        {
                            var firstLetter = maidata.Maidata.Title.ToUpper()[0].ToString();
                            groupNames.Add(firstLetter == "" ? maidata.Maidata.Title[0].ToString() : firstLetter);
                        }
                    }

                    var sortedGroupNames = groupNames.ToList();
                    sortedGroupNames.Sort((x, y) => new NaturalComparer().Compare(x, y));

                    sortedGroupNames.Add("Misc");

                    AddGroupByAbcd(sortedGroupNames.ToArray());

                    break;
            }


            var listItemDataList = new List<LevelListItemData>();
            var categoryDataList = new List<CategoryData>();
            foreach (var group in groups)
            {
                var category = new CategoryData
                {
                    CategoryNameEntryString = group.Item2
                };

                categoryDataList.Add(category);

                var isFirst = true;

                foreach (var item in group.Item1)
                {
                    var data = new LevelListItemData
                    {
                        MaidataReferenceCountPair = item.Item1,
                        DifficultyIndex = item.Item2,
                        Category = category
                    };

                    listItemDataList.Add(data);

                    if (isFirst)
                    {
                        isFirst = false;
                        category.FirstItem = data;
                    }
                }
            }

            return (listItemDataList.ToArray(), categoryDataList.ToArray());

            void AddGroupByAbcd(string[] keys)
            {
                foreach (var key in keys)
                {
                    var alphabetGroup = key switch
                    {
                        "Misc" => _maidataList.Where(x =>
                            !char.IsLetterOrDigit(x.Maidata.Title[0])
                        ).Select(x => (x, 0)).ToList(),
                        _ =>
                            _maidataList.Where(x =>
                                x.Maidata.Title.ToUpper()[0].ToString() == key ||
                                PinyinHelper.GetPinyin(x.Maidata.Title[0]).ToUpper()[0]
                                    .ToString() ==
                                key).Select(x => (x, 0)).ToList()
                    };

                    if (alphabetGroup.Count == 0)
                        continue;

                    alphabetGroup.Sort((x, y) =>
                        new NaturalComparer().Compare(x.Item1.Maidata.Title, y.Item1.Maidata.Title));

                    groups.Add((alphabetGroup.ToArray(), key));
                }
            }

            void AddGroupByDifficulty(string[] keys)
            {
                foreach (var difficultyName in keys)
                {
                    var difficultyGroup =
                        new List<(MaidataReferenceCountPair, int
                            )>(); // _maidataList.Where(x => x.DifficultyNames.Contains(difficulty)).ToList();

                    foreach (var maidata in _maidataList)
                    foreach (var chart in maidata.Maidata.Charts)
                        if (chart.DifficultyString == difficultyName)
                            difficultyGroup.Add((maidata, chart.DifficultyIndex));

                    if (difficultyGroup.Count == 0)
                        continue;

                    difficultyGroup.Sort((x, y) =>
                        new NaturalComparer().Compare(x.Item1.Maidata.Title, y.Item1.Maidata.Title));

                    groups.Add((difficultyGroup.ToArray(), difficultyName));
                }
            }
        }
    }
}