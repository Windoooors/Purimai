using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GihanSoft.String;
using TinyPinyin;
using UnityEngine;

namespace UI.LevelSelection
{
    public static class MaidataManager
    {
        public static readonly List<MaidataReferenceCountPair> MaidataList = new();

        public static (LevelListItemData[], CategoryData[]) GetLevelListItemData(
            LevelSelectionManager.SortingRules rule)
        {
            var groups = new List<((MaidataReferenceCountPair, int)[], string)>();

            switch (rule)
            {
                case LevelSelectionManager.SortingRules.Difficulty:
                    var difficultyStringHashSet = new HashSet<string>();

                    foreach (var maidata in MaidataList)
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
                case LevelSelectionManager.SortingRules.Alphabet:
                    var groupNames = new HashSet<string>();

                    foreach (var maidata in MaidataList)
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
                        "Misc" => MaidataList.Where(x =>
                            !char.IsLetterOrDigit(x.Maidata.Title[0])
                        ).Select(x => (x, 0)).ToList(),
                        _ =>
                            MaidataList.Where(x =>
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

                    foreach (var maidata in MaidataList)
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

        public static void Load(bool clear = false)
        {
            if (MaidataList.Count != 0 && !clear)
                return;

            MaidataList.Clear();

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

                MaidataList.Add(new MaidataReferenceCountPair
                {
                    Maidata = maidata,
                    Referenced = false
                });
            }

            UIManager.Instance.UpdateTMPAtlas(Maidata.UsedCharacters.ToArray());
        }


        private static bool FileExistsIgnoreCase(string input, out string actualPath)
        {
            actualPath = "";

            if (string.IsNullOrEmpty(input)) return false;

            try
            {
                var directory = Path.GetDirectoryName(input);
                var fileName = Path.GetFileName(input);

                if (string.IsNullOrEmpty(directory)) directory = Directory.GetCurrentDirectory();

                if (!Directory.Exists(directory)) return false;

                var matches = Directory.GetFiles(directory, fileName, new EnumerationOptions
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
    }
}