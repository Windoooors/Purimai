using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game;
using GihanSoft.String;
using TinyPinyin;
using UI.GameSettings;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.LevelSelection
{
    public class LevelSelectionController : MonoBehaviour
    {
        public enum SortingRules
        {
            Alphabet,
            Difficulty
        }
        
        private const float ItemHeight = 126;
        private const int VirtualCount = 100000;

        private static LevelSelectionController _instance;

        public SortingRules groupByRule;

        public VisualTreeAsset itemTemplate;

        private readonly List<MaidataReferenceCountPair> _maidataList = new();

        private void Awake()
        {
            _instance = this;

            groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            var path = Path.Combine(Application.persistentDataPath, "Charts/");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var levelPath in Directory.GetDirectories(path))
            {
                if (!(FileExistsIgnoreExtCase(Path.Combine(levelPath, "maidata.txt"), out var actualMaidataPath) &&
                      (FileExistsIgnoreExtCase(Path.Combine(levelPath, "track.mp3"), out var actualSongMp3Path) ||
                       FileExistsIgnoreExtCase(Path.Combine(levelPath, "track.ogg"), out var actualSongOggPath))))
                    continue;

                actualSongOggPath = "";

                var aviExists = FileExistsIgnoreExtCase(Path.Combine(levelPath, "pv.avi"), out var actualPvPathAvi);
                FileExistsIgnoreExtCase(Path.Combine(levelPath, "pv.mp4"), out var actualPvPathMp4);

                var pngExists = FileExistsIgnoreExtCase(Path.Combine(levelPath, "bg.png"), out var actualBgPathPng);
                var jpgExists = FileExistsIgnoreExtCase(Path.Combine(levelPath, "bg.jpg"), out var actualBgPathJpg);

                if (!jpgExists)
                    FileExistsIgnoreExtCase(Path.Combine(levelPath, "bg.jpeg"), out actualBgPathJpg);

                var maidata = new Maidata(actualMaidataPath,
                    File.Exists(actualSongMp3Path) ? actualSongMp3Path : actualSongOggPath,
                    aviExists ? actualPvPathAvi : actualPvPathMp4,
                    pngExists ? actualBgPathPng : actualBgPathJpg);

                _maidataList.Add(new MaidataReferenceCountPair
                {
                    Maidata = maidata,
                    ReferenceCount = 0
                });
            }

            UIManager.GetInstance().UpdateTMPAtlas(Maidata.UsedCharacters.ToArray());

            Initialize();
        }

        private static bool FileExistsIgnoreExtCase(string path, out string actualPath)
        {
            var directory = Path.GetDirectoryName(path);
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            if (!Directory.Exists(directory))
            {
                actualPath = "";
                return false;
            }

            var files = Directory.GetFiles(directory, filenameWithoutExt + ".*");

            foreach (var file in files)
                if (string.Equals(Path.GetExtension(file), ext, StringComparison.OrdinalIgnoreCase))
                {
                    actualPath = file;
                    return true;
                }

            actualPath = "";
            return false;
        }

        private ListView _listView;
        private ScrollView _scrollView;
        private VisualElement _largeSongCover;
        
        private LevelListItemData[] _data;

        public void Initialize()
        {
            groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            var rawData = GetLevelListItemData(groupByRule).ToList();

            var dataList = new List<LevelListItemData>();

            while (dataList.Count < VirtualCount)
            {
                dataList.AddRange(rawData);
            }

            _data = dataList.ToArray();

            /*Initialize List*/

            var root = GetComponent<UIDocument>().rootVisualElement;
            _listView = root.Q<VisualElement>("list-parent").Q<ListView>("list");

            _scrollView = _listView.Q<ScrollView>();

            _scoreContentPanel = root.Q<ScoreContentPanel>();

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
                songTitleWaterMark.text = maidata.Maidata.Title;

                var artistLabel = informationElement.Q<Label>("song-artist");
                var bpmLabel = informationElement.Q<Label>("song-bpm");

                artistLabel.text = maidata.Maidata.Artist;
                bpmLabel.text = maidata.Maidata.Bpm.ToString("");

                maidata.ReferenceCount++;

                if (!maidata.Maidata.CoverDataLoaded)
                {
                    Task.Run(() => { maidata.Maidata.LoadSongCover(); });
                }

                element.userData = index;

                SetStyle();
                element.style.visibility = Visibility.Visible;

                element.RemoveFromClassList("enable-transition");

                element.UnregisterCallback<PointerUpEvent>(OnItemClicked);
                element.RegisterCallback<PointerUpEvent>(OnItemClicked);
            };

            _listView.unbindItem = (element, index) =>
            {
                var maidata = _data[index].MaidataReferenceCountPair;

                maidata.ReferenceCount--;

                element.RemoveFromClassList("enable-transition");

                element.RemoveFromClassList("show-cover");

                if (maidata.ReferenceCount <= 0)
                {
                    maidata.ReferenceCount = 0;
                    maidata.Maidata.UnloadSongCover();
                }

                element.style.visibility = Visibility.Hidden;
            };

            _listView.selectionChanged += _ =>
            {
                _listView.Query<TemplateContainer>().ForEach(item =>
                    {
                        if (!item.ClassListContains("enable-transition"))
                        {
                            item.AddToClassList("enable-transition");
                        }
                    }
                );

                if (_songCoverPreviewApplied)
                {
                    _data[_listView.selectedIndex].MaidataReferenceCountPair.ReferenceCount--;
                    _songCoverPreviewApplied = false;
                }
                
                _scoreContentPanel.SetChartInformationData(_data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata, _data[_listView.selectedIndex].DifficultyIndex);
                _scoreContentPanel.SetScoreData(
                    ChartRankDataManager.GetChartRankData(_data[_listView.selectedIndex].MaidataReferenceCountPair
                        .Maidata.MaidataDirectoryName),  _data[_listView.selectedIndex].DifficultyIndex);

                AudioManager.GetInstance().PlaySelectSound();
            };

            _snap = new SnapScrollManipulator(ItemHeight, 741);
            _scrollView.AddManipulator(_snap);

            _scrollView.verticalScroller.valueChanged += _ => { SetStyle(); };

            _listView.selectionType = SelectionType.None;

            _largeSongCover = root.Q<VisualElement>("song-cover").Q<VisualElement>("song-cover-image");

            /*Generic Initialization*/

            _listView.itemsSource = _data;

            _snap.SnapToItem(VirtualCount / 2, _scrollView, false);
        }

        private SnapScrollManipulator _snap;
        private ScoreContentPanel _scoreContentPanel;

        private void OnItemClicked(PointerUpEvent pointerUpEvent)
        {
            var element = pointerUpEvent.currentTarget as VisualElement;
            
            var index = (int)(element?.userData ?? 0);
            
            if (_listView.selectedIndex != index)
            {
                _snap.SnapToItem(index, _scrollView);
                return;
            }
            
            var root = GetComponent<UIDocument>().rootVisualElement;
                
            root.AddToClassList("enter-level");
            
            Invoke(nameof(EnterLevel), 0.5f);
        }

        private void EnterLevel()
        {
            LevelLoader.GetInstance.EnterLevel(_data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata, _data[_listView.selectedIndex].DifficultyIndex);

            UIManager.GetInstance().uIDocument.enabled = false;
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

            var maidata = _data[index].MaidataReferenceCountPair.Maidata;
            
            if (maidata.CoverDataLoaded)
            {
                var songCoverElement = item.Q<VisualElement>("song-item").Q<VisualElement>("song-cover");
                var tex = maidata.SongCoverDecodedImage.GetTexture2D();
                songCoverElement.style.backgroundImage = new StyleBackground(tex);

                if (!item.ClassListContains("show-cover"))
                    item.AddToClassList("show-cover");
            }

            if (relativeY is < 750 and > 730)
            {
                _listView.selectedIndex = (int)item.userData;
            }
        }
        
        private void Update()
        {
            SetStyle();

            if (_listView.selectedIndex >= _data.Length || _listView.selectedIndex < 0)
                return;
            
            var maidata = _data[_listView.selectedIndex].MaidataReferenceCountPair.Maidata;
            
            if (maidata.CoverDataLoaded && !_songCoverPreviewApplied)
            {
                _largeSongCover.style.backgroundImage =
                    new StyleBackground(maidata.SongCoverDecodedImage.GetTexture2D());

                _songCoverPreviewApplied = true;
                _data[_listView.selectedIndex].MaidataReferenceCountPair.ReferenceCount++;
            }
        }

        private bool _songCoverPreviewApplied;

        public static LevelSelectionController GetInstance()
        {
            return _instance ? _instance : FindAnyObjectByType<LevelSelectionController>();
        }

        public LevelListItemData[] GetLevelListItemData(SortingRules rule)
        {
            var groups = new List<((MaidataReferenceCountPair, int)[], string)>();

            switch (rule)
            {
                case SortingRules.Difficulty:
                    var difficultyStringHashSet = new HashSet<string>();

                    foreach (var maidata in _maidataList)
                        for (var i = 0; i < maidata.Maidata.Difficulties.Length; i++)
                        {
                            var difficultyName = maidata.Maidata.Difficulties[i];

                            if (maidata.Maidata.Charts[i] == string.Empty)
                                continue;

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

                    sortedGroupNames.Add("Miscellaneous");

                    AddGroupByAbcd(sortedGroupNames.ToArray());

                    break;
            }


            var listItemDataList = new List<LevelListItemData>();
            foreach (var group in groups)
            {
                var category = new CategoryData
                {
                    CategoryNameEntryString = group.Item2
                };

                foreach (var item in group.Item1)
                    listItemDataList.Add(new LevelListItemData
                    {
                        MaidataReferenceCountPair = item.Item1,
                        DifficultyIndex = item.Item2,
                        Category = category
                    });
            }

            return listItemDataList.ToArray();

            void AddGroupByAbcd(string[] keys)
            {
                foreach (var key in keys)
                {
                    var alphabetGroup = key switch
                    {
                        "Miscellaneous" => _maidataList.Where(x =>
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

                    var validFilteredGroup = alphabetGroup.Where(x =>
                    {
                        var validCharts = 0;

                        for (var i = 0; i < 6; i++)
                            if (x.Item1.Maidata.Charts[i] != "")
                                validCharts++;

                        return validCharts > 0;
                    });

                    groups.Add((validFilteredGroup.ToArray(), key));
                }
            }

            void AddGroupByDifficulty(string[] keys)
            {
                foreach (var difficulty in keys)
                {
                    var difficultyGroup =
                        new List<(MaidataReferenceCountPair, int
                            )>(); // _maidataList.Where(x => x.DifficultyNames.Contains(difficulty)).ToList();

                    foreach (var maidata in _maidataList)
                        for (var i = 0; i < maidata.Maidata.Difficulties.Length; i++)
                            if (maidata.Maidata.Difficulties[i] == difficulty &&
                                maidata.Maidata.Charts[i] != string.Empty)
                                difficultyGroup.Add((maidata, i));

                    if (difficultyGroup.Count == 0)
                        continue;

                    difficultyGroup.Sort((x, y) =>
                        new NaturalComparer().Compare(x.Item1.Maidata.Title, y.Item1.Maidata.Title));

                    groups.Add((difficultyGroup.ToArray(), difficulty));
                }
            }
        }
    }
}