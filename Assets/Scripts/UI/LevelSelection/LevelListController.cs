using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GihanSoft.String;
using TinyPinyin;
using UI.GameSettings;
using UnityEngine;
using UnityEngine.Serialization;
using Image = UnityEngine.UI.Image;

namespace UI.LevelSelection
{
    public class LevelListController : MonoBehaviour
    {
        public enum SortingRules
        {
            Alphabet,
            Difficulty
        }

        private static LevelListController _instance;

        public List levelList;
        public LevelListItem levelItemPrefab;
        public SortingRules groupByRule;

        public CanvasGroup levelSelectionUiLayer;

        [FormerlySerializedAs("songCoverBackground")]
        public Image songCoverBackgroundImage;
        
        public Image backgroundImage;

        private readonly List<Maidata> _maidataList = new();

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

                _maidataList.Add(maidata);
            }

            UIManager.GetInstance().UpdateTMPAtlas(Maidata.UsedCharacters.ToArray());
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

        public void Initialize()
        {
            groupByRule = SettingsPool.GetValue("song_list.group_rule") switch
            {
                0 => SortingRules.Alphabet,
                _ => SortingRules.Difficulty
            };

            levelList.Initialize(GetLevelListItemData(groupByRule), levelItemPrefab);
        }

        public void ShowButton()
        {
            var upArrowButton = Button.GetButton(0);
            upArrowButton.ChangeIcon(UIManager.GetInstance().buttonIcons.upArrow);
            upArrowButton.Show();

            var downArrowButton = Button.GetButton(3);
            downArrowButton.ChangeIcon(UIManager.GetInstance().buttonIcons.downArrow);
            downArrowButton.Show();

            var settingsButton = Button.GetButton(7);
            settingsButton.ChangeIcon(UIManager.GetInstance().buttonIcons.settings);
            settingsButton.Show();

            var playButton = Button.GetButton(4);
            playButton.ChangeIcon(UIManager.GetInstance().buttonIcons.play);
            playButton.Show();

            var levelUpButton = Button.GetButton(2);
            levelUpButton.ChangeIcon(UIManager.GetInstance().buttonIcons.levelUp);
            levelUpButton.Show();

            var levelDownButton = Button.GetButton(1);
            levelDownButton.ChangeIcon(UIManager.GetInstance().buttonIcons.levelDown);
            levelDownButton.Show();
            
            var versionButton = Button.GetButton(5);
            var scoreButton = Button.GetButton(6);
            
            var versionIsFinale = SettingsPool.GetValue("game.achievement_type") == 0;
            var showScore = SettingsPool.GetValue("game.score_indicator_type") == 0;

            var buttonIconSet = UIManager.GetInstance().buttonIcons;

            versionButton.ChangeIcon(versionIsFinale ? buttonIconSet.finale : buttonIconSet.dx);
            scoreButton.ChangeIcon(showScore ? buttonIconSet.score : buttonIconSet.achievement);
            
            versionButton.Show();
            scoreButton.Show();
        }

        public static LevelListController GetInstance()
        {
            return _instance ? _instance : FindAnyObjectByType<LevelListController>();
        }

        public ItemDataBase[] GetLevelListItemData(SortingRules rule)
        {
            var groups = new List<((Maidata, int)[], string)>();

            switch (rule)
            {
                case SortingRules.Difficulty:
                    var difficultyStringHashSet = new HashSet<string>();

                    foreach (var maidata in _maidataList)
                        for (var i = 0; i < maidata.Difficulties.Length; i++)
                        {
                            var difficultyName = maidata.Difficulties[i];

                            if (maidata.Charts[i] == string.Empty)
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
                        var firstCharacterIsLetterOrDigit = char.IsLetterOrDigit(maidata.Title[0]);
                        var firstCharacterIsInChinese =
                            PinyinHelper.IsChinese(maidata.Title[0]);
                        var pinyinOfFirstCharacter =
                            PinyinHelper.GetPinyin(maidata.Title[0]);

                        if (firstCharacterIsInChinese)
                        {
                            groupNames.Add(pinyinOfFirstCharacter.ToUpper()[0].ToString());
                        }
                        else if (firstCharacterIsLetterOrDigit)
                        {
                            var firstLetter = maidata.Title.ToUpper()[0].ToString();
                            groupNames.Add(firstLetter == "" ? maidata.Title[0].ToString() : firstLetter);
                        }
                    }

                    var sortedGroupNames = groupNames.ToList();
                    sortedGroupNames.Sort((x, y) => new NaturalComparer().Compare(x, y));

                    sortedGroupNames.Add("Miscellaneous");

                    AddGroupByAbcd(sortedGroupNames.ToArray());

                    break;
            }


            var listItemDataList = new List<ItemDataBase>();
            foreach (var group in groups)
            {
                listItemDataList.Add(new TitleListItem.TitleData { CategoryNameEntryString = group.Item2 });
                foreach (var item in group.Item1)
                    listItemDataList.Add(new LevelListItemData
                    {
                        Maidata = item.Item1,
                        DifficultyIndex = item.Item2
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
                            !char.IsLetterOrDigit(x.Title[0])
                        ).Select(x => (x, 0)).ToList(),
                        _ =>
                            _maidataList.Where(x =>
                                x.Title.ToUpper()[0].ToString() == key ||
                                PinyinHelper.GetPinyin(x.Title[0]).ToUpper()[0]
                                    .ToString() ==
                                key).Select(x => (x, 0)).ToList()
                    };

                    if (alphabetGroup.Count == 0)
                        continue;

                    alphabetGroup.Sort((x, y) => new NaturalComparer().Compare(x.Item1.Title, y.Item1.Title));

                    var validFilteredGroup = alphabetGroup.Where(x =>
                    {
                        var validCharts = 0;

                        for (var i = 0; i < 6; i++)
                            if (x.Item1.Charts[i] != "")
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
                        new List<(Maidata, int)>(); // _maidataList.Where(x => x.DifficultyNames.Contains(difficulty)).ToList();

                    foreach (var maidata in _maidataList)
                        for (var i = 0; i < maidata.Difficulties.Length; i++)
                            if (maidata.Difficulties[i] == difficulty && maidata.Charts[i] != string.Empty)
                                difficultyGroup.Add((maidata, i));

                    if (difficultyGroup.Count == 0)
                        continue;

                    difficultyGroup.Sort((x, y) => new NaturalComparer().Compare(x.Item1.Title, y.Item1.Title));

                    groups.Add((difficultyGroup.ToArray(), difficulty));
                }
            }
        }
    }
}