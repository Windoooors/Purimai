using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GihanSoft.String;
using TinyPinyin;
using UnityEngine;

namespace UI.LevelSelection
{
    public class LevelListController : MonoBehaviour
    {
        public enum SortingRules
        {
            Alphabet,
            Difficulty
        }

        public static LevelListController Instance;

        public List levelList;
        public LevelListItem levelItemPrefab;
        public SortingRules groupByRule;

        private readonly List<Maidata> _maidataList = new();

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
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

                var maidata = new Maidata(maidataPath, string.Equals(songPathMp3, "") ? songPathOgg : songPathMp3,
                    string.Equals(pvPathMp4, "") ? pvPathAvi : pvPathMp4,
                    string.Equals(bgPathPng, "") ? bgPathJpg : bgPathPng);

                _maidataList.Add(maidata);
            }

            levelList.Initialize(GetLevelListItemData(groupByRule), levelItemPrefab);
        }

        public ItemDataBase[] GetLevelListItemData(SortingRules rule)
        {
            var groups = new List<((Maidata,int)[], string)>();

            switch (rule)
            {
                case SortingRules.Difficulty:
                    var difficultyStringHashSet = new HashSet<string>();

                    foreach (var maidata in _maidataList)
                        for (var i = 0; i < maidata.DifficultyNames.Length; i++)
                        {
                            var difficultyName = maidata.DifficultyNames[i];

                            if (difficultyName == "" &&
                                maidata.Designers[i] == "")
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
                listItemDataList.Add(new TitleListItem.TitleData { CategoryName = group.Item2 });
                foreach (var item in group.Item1)
                    listItemDataList.Add(new LevelListItemData
                    {
                        LevelName = item.Item1.Title,
                        ChartData = item.Item1,
                        DefaultDifficultyIndex = item.Item2
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
                        ).Select(x=>(x,0)).ToList(),
                        _ =>
                            _maidataList.Where(x =>
                                x.Title.ToUpper()[0].ToString() == key ||
                                PinyinHelper.GetPinyin(x.Title[0]).ToUpper()[0]
                                    .ToString() ==
                                key).Select(x=>(x,0)).ToList()
                    };

                    if (alphabetGroup.Count == 0)
                        continue;

                    alphabetGroup.Sort((x, y) => new NaturalComparer().Compare(x.Item1.Title, y.Item1.Title));

                    groups.Add((alphabetGroup.ToArray(), key));
                }
            }

            void AddGroupByDifficulty(string[] keys)
            {
                foreach (var difficulty in keys)
                {
                    var difficultyGroup = new List<(Maidata, int)>(); // _maidataList.Where(x => x.DifficultyNames.Contains(difficulty)).ToList();

                    foreach (var maidata in _maidataList)
                    {
                        for (var i = 0; i < maidata.DifficultyNames.Length; i++)
                        {
                            if (maidata.DifficultyNames[i] == difficulty)
                            {
                                difficultyGroup.Add((maidata,i));
                            }
                        }
                    }
                    
                    
                    if (difficultyGroup.Count == 0)
                        continue;

                    difficultyGroup.Sort((x, y) => new NaturalComparer().Compare(x.Item1.Title, y.Item1.Title));
                    
                    groups.Add((difficultyGroup.ToArray(), difficulty));
                }
            }
        }

        public class Maidata
        {
            public readonly string Artist;
            public readonly string BgPath;
            public readonly float Bpm;
            public readonly string[] Charts;
            public readonly string[] Designers;

            public readonly string[] DifficultyNames;

            public readonly float FirstNoteTime;
            public readonly string Genre;
            public readonly string MainChartDesigner;
            public readonly string PvPath;
            public readonly string SongPath;

            public readonly string Title;

            public Maidata(string maidataPath, string songPath, string pvPath, string bgPath)
            {
                SongPath = songPath;
                PvPath = pvPath;
                BgPath = bgPath;

                var maidata = File.ReadAllText(maidataPath);

                var titleMatch = new Regex("&title=(.*)").Match(maidata);
                var artistMatch = new Regex("&artist=(.*)").Match(maidata);
                var mainDesignerMatch = new Regex("&des=(.*)").Match(maidata);
                var genreMatch = new Regex("&genre=(.*)").Match(maidata);
                var firstNoteTimeMatch = new Regex(@"&first=(\d+.\d+|\d+)").Match(maidata);
                var bpmMatch = new Regex(@"&wholebpm=(\d+.\d+|\d+)").Match(maidata);

                Title = titleMatch.Success ? titleMatch.Groups[1].Value : "未知";
                Artist = artistMatch.Success ? artistMatch.Groups[1].Value : "未知";
                MainChartDesigner = mainDesignerMatch.Success ? mainDesignerMatch.Groups[1].Value : "未知";
                Genre = genreMatch.Success ? genreMatch.Groups[1].Value : "未知";
                Bpm = bpmMatch.Success ? float.Parse(bpmMatch.Groups[1].Value) : 0;
                FirstNoteTime = firstNoteTimeMatch.Success ? float.Parse(firstNoteTimeMatch.Groups[1].Value) : 0;

                var difficultyNameList = new List<string>();
                var designerList = new List<string>();
                var chartList = new List<string>();

                for (var i = 1; i <= 6; i++)
                {
                    var levelRegex = new Regex($"&lv_{i}=(.*)");
                    var designerRegex = new Regex($"&des_{i}=(.*)");

                    var levelMatch = levelRegex.IsMatch(maidata) ? levelRegex.Match(maidata).Groups[1].Value : "";
                    var designerMatch = designerRegex.IsMatch(maidata)
                        ? designerRegex.Match(maidata).Groups[1].Value
                        : "";

                    difficultyNameList.Add(levelMatch);
                    designerList.Add(designerMatch);

                    var chartRegex = new Regex($@"&inote_{i}=((?s).*?)(?=E|&inote_{i + 1}|\z)");

                    if (!chartRegex.IsMatch(maidata))
                    {
                        chartList.Add("");
                        continue;
                    }

                    if (designerList[^1] == "")
                        designerList[^1] = MainChartDesigner;
                    chartList.Add(chartRegex.Match(maidata).Groups[1].Value);
                }

                DifficultyNames = difficultyNameList.ToArray();
                Charts = chartList.ToArray();
                Designers = designerList.ToArray();
            }
        }
    }
}