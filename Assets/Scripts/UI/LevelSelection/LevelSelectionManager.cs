using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Settings;
using UI.Settings.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.LevelSelection
{
    public class LevelSelectionManager : MonoBehaviour
    {
        public enum SortingRules
        {
            Alphabet,
            Difficulty,
            Undefined
        }

        private const float ItemHeight = 126;
        private const int VirtualCount = 100000;

        private static LevelSelectionManager _instance;

        public SortingRules groupByRule = SortingRules.Undefined;

        public VisualTreeAsset itemTemplate;

        public VisualTreeAsset levelSelectionTreeAsset;

        public LevelLoader levelLoader;

        public bool songPreviewing = true;

        private LevelListItemData[] _data;
        private VisualElement _largeSongCover;

        private CategoryData _lastCategoryData;

        private Maidata _lastPreviewedMaidata;

        private ListView _listView;
        private Button _modsButton;
        private LevelListItemData[] _rawData;
        private Button _refreshButton;
        private ScoreContentPanel _scoreContentPanel;
        private ScrollView _scrollView;
        private Button _settingsButton;

        private SnapScrollManipulator _snapManipulator;
        private SongCoverManipulator _songCoverManipulator;

        private bool _songPlaying;

        private Button _sortButton;

        public VisualElement LevelSelectionTree;

        public static LevelSelectionManager Instance => _instance ?? FindAnyObjectByType<LevelSelectionManager>();

        private void Awake()
        {
            LevelSelectionTree = levelSelectionTreeAsset.Instantiate();

            LevelSelectionTree.style.position = Position.Absolute;
            LevelSelectionTree.style.left = 0;
            LevelSelectionTree.style.top = 0;
            LevelSelectionTree.style.bottom = 0;
            LevelSelectionTree.style.right = 0;

            UIManager.Instance.uiDocument.rootVisualElement.Add(LevelSelectionTree);

            _instance = this;

            MaidataManager.Load();

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

            foreach (var pair in MaidataManager.MaidataList)
                if (!pair.Referenced && pair.Maidata.CoverDataLoaded)
                    pair.Maidata.UnloadSongCover();

            if (maidata.SongLoaded && !_songPlaying && songPreviewing)
            {
                _songPlaying = true;

                maidata.SongBassHandler.PlayOneShot();
            }

            if (_songPlaying && maidata.SongBassHandler?.IsPlaying == false && songPreviewing)
                maidata.SongBassHandler.PlayOneShot();
        }

        private void OnDestroy()
        {
            SettingsManager.OnSettingsChanged -= InitializeGroupingRule;
            SettingsManager.OnSettingsChanged -= ChangeVolume;

            _scrollView.RemoveManipulator(_snapManipulator);
            _largeSongCover.RemoveManipulator(_songCoverManipulator);

            UIManager.Instance.uiDocument?.rootVisualElement?.Remove(LevelSelectionTree);
        }

        private void OnApplicationQuit()
        {
            var index = _listView.selectedIndex % _rawData.Length;

            PlayerPrefs.SetInt("LevelListIndex", index);
        }

        private void ChangeVolume()
        {
            var volume = SettingsPool.GetValue("volume.song") / 10f;

            if (_lastPreviewedMaidata != null)
                _lastPreviewedMaidata.SongBassHandler.Volume = volume;
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
            ScreenOrientationManager.Instance.DisablePortrait();

            var root = LevelSelectionTree;
            _listView = root.Q<VisualElement>("list-parent").Q<ListView>("list");

            _listView.TrySetTouchDraggingAllowed(true);

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

            CategoryListManager.Instance.Initialize();
            CategoryListManager.Instance.OnCategoryTendsToChange += ChangeCategory;

            _largeSongCover = root.Q<VisualElement>("song-cover").Q<VisualElement>("song-cover-image");

            _songCoverManipulator =
                new SongCoverManipulator(SongCoverManipulator.SongCoverLayoutPopulationMode.FixedHeight, 0);

            _largeSongCover.AddManipulator(_songCoverManipulator);

            _sortButton = root.Q<VisualElement>("control-panel").Q<Button>("sort-button");
            _sortButton.clicked += ChangeGroupingRule;
            SettingsManager.OnSettingsChanged += InitializeGroupingRule;
            SettingsManager.OnSettingsChanged += ChangeVolume;

            _settingsButton = root.Q<VisualElement>("control-panel").Q<Button>("settings-button");
            _settingsButton.clicked += UIManager.Instance.ShowSettingsPanel;

            _refreshButton = root.Q<VisualElement>("control-panel").Q<Button>("refresh-button");
            _refreshButton.clicked += () =>
            {
                MaidataManager.Load(true);
                InitializeGroupingRuleCore();
            };

            _modsButton = root.Q<VisualElement>("control-panel").Q<Button>("mods-button");
            _modsButton.clicked += UIManager.Instance.ShowModsPanel;

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
            };

            _snapManipulator.OnSnapToItem += (_, args) =>
            {
                LoadSong(args.TargetIndex);

                if (!args.IsByHand)
                    return;

                if (_lastCategoryData != _data[args.TargetIndex].Category)
                    CategoryListManager.Instance.ChangeCategoryPassively(_data[args.TargetIndex].Category);

                _lastCategoryData = _data[args.TargetIndex].Category;
            };
        }

        private void LoadSong(int index)
        {
            if (_lastPreviewedMaidata != null && _lastPreviewedMaidata ==
                _data[index].MaidataReferenceCountPair.Maidata)
                return;

            _lastPreviewedMaidata?.UnloadSong();

            _data[index].MaidataReferenceCountPair.Maidata.LoadSongClip();
            _songPlaying = false;

            _lastPreviewedMaidata = _data[index].MaidataReferenceCountPair.Maidata;
        }

        private void InitializeGroupingRuleCore()
        {
            var currentData = _data?[_listView.selectedIndex];

            var pairedData = MaidataManager.GetLevelListItemData(groupByRule);

            _rawData = pairedData.Item1;

            var dataList = new List<LevelListItemData>();

            Logging.Logger.LogInfo($"Grouped data count: {_rawData.Length}");

            while (dataList.Count < VirtualCount) dataList.AddRange(_rawData);

            _data = dataList.ToArray();

            try
            {
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

                CategoryListManager.Instance.ChangeData(pairedData.Item2);

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
                    CategoryListManager.Instance.ChangeCategoryPassively(_data[targetIndex].Category);

                _lastCategoryData = _data[targetIndex].Category;
            }
            catch (Exception ex)
            {
                Logging.Logger.LogError($"{ex.Message} Stack Trace: {ex.StackTrace}");
            }
        }

        private void InitializeGroupingRule()
        {
            Logging.Logger.LogInfo("Initializing grouping rule.");
            
            var newGroupByRule = SettingsPool.GetValue("group_rule") switch
            {
                0 => SortingRules.Alphabet,
                1 => SortingRules.Difficulty,
                _ => SortingRules.Undefined
            };

            if (groupByRule == newGroupByRule)
                return;

            groupByRule = newGroupByRule;

            InitializeGroupingRuleCore();
        }

        private void ChangeGroupingRule()
        {
            groupByRule = SettingsPool.GetValue("group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            SettingsPool.SetValue("group_rule", groupByRule is SortingRules.Alphabet ? 1 : 0);

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
                LevelSelectionTree.styleSheets.Add(
                    Resources.Load<StyleSheet>("UI/USS/LevelSelection/LevelSelectionToGameInAnimated"));
                StartCoroutine(EnterLevel());
            };

            return;

            IEnumerator EnterLevel()
            {
                yield return new WaitForSeconds(0.5f);

                LevelLoader.Instance.SceneLoaded += () =>
                {
                    LevelLoader.Instance.SceneLoaded = null;

                    StartCoroutine(PlayWhiteOutAnimation());
                };

                CategoryListManager.Instance.enabled = false;
                _listView.unbindItem = null;

                _snapManipulator.OnSnapToItem = null;

                LevelLoader.Instance.EnterLevel(_data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata,
                    groupByRule == SortingRules.Difficulty
                        ? _data[_listView.selectedIndex].DifficultyIndex
                        : _scoreContentPanel.AlphabeticallySelectedDifficultyIndex);
            }

            IEnumerator PlayWhiteOutAnimation()
            {
                yield return new WaitForSeconds(0.1f);

                LevelSelectionTree.styleSheets.Add(
                    Resources.Load<StyleSheet>("UI/USS/LevelSelection/LevelSelectionToGameOutAnimated"));

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
    }
}