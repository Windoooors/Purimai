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
        }

        private void OnEnable()
        {
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
                var maidataPath = Path.Combine(levelPath, "maidata.txt");
                var songPathMp3 = Path.Combine(levelPath, "track.mp3");
                var songPathOgg = Path.Combine(levelPath, "track.ogg");
                var pvPathMp4 = Path.Combine(levelPath, "pv.mp4");
                var pvPathAvi = Path.Combine(levelPath, "pv.avi");
                var bgPathPng = Path.Combine(levelPath, "bg.png");
                var bgPathJpg = Path.Combine(levelPath, "bg.jpg");

                if (!(File.Exists(maidataPath) && (File.Exists(songPathMp3) || File.Exists(songPathOgg))))
                    continue;

                var maidata = new Maidata(maidataPath, File.Exists(songPathMp3) ? songPathMp3 : songPathOgg,
                    File.Exists(pvPathAvi) ? pvPathAvi : pvPathMp4,
                    File.Exists(bgPathPng) ? bgPathPng : bgPathJpg);

                _maidataList.Add(maidata);
            }

            UIManager.GetInstance().UpdateTMPAtlas(Maidata.UsedCharacters.ToArray());
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
                            if (!(x.Item1.Charts[i] == "" && x.Item1.Designers[i] == "" &&
                                  x.Item1.Difficulties[i] == ""))
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