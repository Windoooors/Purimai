using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UI.Settings;
using UnityEngine;

namespace UI
{
    public class Maidata
    {
        private static readonly Regex TitleRegex = new("&title=(.*)");
        private static readonly Regex ArtistRegex = new("&artist=(.*)");
        private static readonly Regex MainDesignerRegex = new("&des=(.*)");
        private static readonly Regex GenreRegex = new("&genre=(.*)");
        private static readonly Regex FirstNoteTimeRegex = new(@"&first=((\-?)\d+.\d+|(\-?)\d+)");
        private static readonly Regex BpmRegex = new(@"&wholebpm=(\d+.\d+|\d+)");

        private static readonly Regex UtageDifficultyRegex =
            new(@"([0-9]+\+?\?|[\u4E00-\u9FFF]\s?[0-9]+\+?\??|utage\s?[0-9]+\+?\??)");

        private static readonly Regex UtageTitleRegex =
            new(@"^\[.*?\]");

        public static HashSet<char> UsedCharacters = new();

        private readonly string _songCoverPath;
        public readonly string Artist;

        public readonly float Bpm;

        public readonly float FirstNoteTime;
        public readonly string Genre;

        public readonly bool IsUtage;
        public readonly string MaidataDirectoryName;
        public readonly string MainChartDesigner;
        public readonly string PvPath;
        public readonly string SongPath;

        public readonly string Title;

        private bool _generatingBlurredCover;

        private bool _loadingCover;
        private bool _loadingSong;
        public DecodedImage BlurredSongCoverAsBackgroundDecodedImage;
        public DecodedImage BlurredSongCoverDecodedImage;
        public bool BlurredSongCoverGenerated;

        public Chart[] Charts;
        public bool CoverDataLoaded;

        public AudioClip SongAudioClip;

        public DecodedImage SongCoverDecodedImage;
        public bool SongLoaded;

        public Maidata(string maidataPath, string songPath, string pvPath, string songCoverPath)
        {
            MaidataDirectoryName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(maidataPath));
            SongPath = songPath;
            PvPath = pvPath;
            _songCoverPath = songCoverPath;

            var maidata = File.ReadAllText(maidataPath);

            var titleMatch = TitleRegex.Match(maidata);
            var artistMatch = ArtistRegex.Match(maidata);
            var mainDesignerMatch = MainDesignerRegex.Match(maidata);
            var genreMatch = GenreRegex.Match(maidata);
            var firstNoteTimeMatch = FirstNoteTimeRegex.Match(maidata);
            var bpmMatch = BpmRegex.Match(maidata);

            Title = titleMatch.Success ? titleMatch.Groups[1].Value : "Unknown";
            Artist = artistMatch.Success ? artistMatch.Groups[1].Value : "Unknown";
            MainChartDesigner = mainDesignerMatch.Success ? mainDesignerMatch.Groups[1].Value : "Unknown";
            Genre = genreMatch.Success ? genreMatch.Groups[1].Value : "Unknown";
            Bpm = bpmMatch.Success ? float.Parse(bpmMatch.Groups[1].Value) : -1;
            FirstNoteTime = firstNoteTimeMatch.Success ? float.Parse(firstNoteTimeMatch.Groups[1].Value) : 0;

            AddUsedCharacters(Title);
            AddUsedCharacters(Artist);
            AddUsedCharacters(MainChartDesigner);
            AddUsedCharacters(Genre);

            var chartList = new List<Chart>();

            if (!HasChart(maidata))
            {
                Charts = Array.Empty<Chart>();
                return;
            }

            var i = 0;

            var found = TryGetChartString(maidata, i, out var chartString, out var isLast);

            while (true)
            {
                if (!found)
                {
                    i++;
                    found = TryGetChartString(maidata, i, out chartString, out isLast);

                    continue;
                }

                var chart = new Chart();

                chart.ChartString = chartString;

                if (!TryGetDesigner(maidata, i, out var designer))
                    designer = MainChartDesigner;

                chart.Designer = designer;

                if (!TryGetLevel(maidata, i, out var level))
                    level = "NaN";

                chart.DifficultyString = level;

                chart.DifficultyIndex = i;

                if (UtageDifficultyRegex.IsMatch(level.ToLower()) || UtageTitleRegex.IsMatch(Title))
                    IsUtage = true;

                AddUsedCharacters(level);
                AddUsedCharacters(designer);

                chartList.Add(chart);

                if (isLast)
                    break;

                i++;
                found = TryGetChartString(maidata, i, out chartString, out isLast);
            }

            if (chartList.Count == 0)
            {
                Charts = Array.Empty<Chart>();
                return;
            }

            Charts = chartList.ToArray();

            if (Bpm.CompareTo(-1) == 0 && Charts.Length > 0)
            {
                var splitResult = Charts[^1].ChartString.TrimStart().Split(")");
                if (splitResult.Length < 1 || splitResult[0].Length < 2)
                {
                    Bpm = 0;
                    return;
                }

                Bpm = float.TryParse(Charts[^1].ChartString.TrimStart().Split(")")[0].Substring(1),
                    out var defaultBpm)
                    ? defaultBpm
                    : 0;
            }
        }

        private bool HasChart(string maidataString)
        {
            if (string.IsNullOrEmpty(maidataString)) return false;

            var span = maidataString.AsSpan();
            var pos = 0;

            // 循环查找关键字 "&inote_"
            while ((pos = maidataString.IndexOf("&inote_", pos, StringComparison.Ordinal)) != -1)
            {
                var numStart = pos + 7; // "&inote_".Length

                // 边界检查：确保关键字后至少还有一个字符（数字）
                if (numStart < span.Length)
                    // 模式匹配：[数字]+=
                    // 1. 紧随其后的第一个字符必须是数字
                    if (char.IsDigit(span[numStart]))
                    {
                        // 2. 寻找后续的 '='
                        var eqIdx = maidataString.IndexOf('=', numStart);
                        if (eqIdx != -1)
                        {
                            // 3. 验证数字与 '=' 之间是否全是数字 (符合 \d+)
                            var isValidNumber = true;
                            for (var j = numStart + 1; j < eqIdx; j++)
                                if (!char.IsDigit(span[j]))
                                {
                                    isValidNumber = false;
                                    break;
                                }

                            if (isValidNumber) return true; // 找到第一个符合条件的就立刻退出
                        }
                    }

                // 未匹配成功，跳过当前前缀继续寻找下一个
                pos += 7;
            }

            return false;
        }

        public bool TryGetLevel(string input, int index, out string level)
        {
            return TryGetField(input, $"&lv_{index}=", out level);
        }

        public bool TryGetDesigner(string input, int index, out string designer)
        {
            return TryGetField(input, $"&des_{index}=", out designer);
        }

        private bool TryGetField(string input, string prefix, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(input)) return false;

            var startIndex = input.IndexOf(prefix, StringComparison.Ordinal);
            if (startIndex == -1) return false;

            var valueStart = startIndex + prefix.Length;

            var lineEndIndex = input.IndexOfAny(new[] { '\r', '\n' }, valueStart);

            if (lineEndIndex == -1)
                value = input.Substring(valueStart);
            else
                value = input.Substring(valueStart, lineEndIndex - valueStart);

            value = value.Trim();
            return true;
        }

        public void UnloadSongCover()
        {
            if (SongCoverDecodedImage == null)
            {
                CoverDataLoaded = false;
                return;
            }

            SongCoverDecodedImage.Dispose();
            SongCoverDecodedImage = null;
            CoverDataLoaded = false;
        }

        public void UnloadSong()
        {
            if (SongAudioClip)
                MonoBehaviour.Destroy(SongAudioClip);

            SongAudioClip = null;
            SongLoaded = false;
        }

        public void UnloadResources()
        {
            if (SongAudioClip)
                MonoBehaviour.Destroy(SongAudioClip);

            if (BlurredSongCoverAsBackgroundDecodedImage != null)
                BlurredSongCoverAsBackgroundDecodedImage.Dispose();

            if (BlurredSongCoverDecodedImage != null)
                BlurredSongCoverDecodedImage.Dispose();

            if (SongCoverDecodedImage != null)
                SongCoverDecodedImage.Dispose();

            BlurredSongCoverAsBackgroundDecodedImage = null;
            BlurredSongCoverDecodedImage = null;
            SongCoverDecodedImage = null;
            SongAudioClip = null;

            BlurredSongCoverGenerated = false;
            CoverDataLoaded = false;
            SongLoaded = false;
        }

        private static void AddUsedCharacters(string usedCharacters)
        {
            foreach (var character in usedCharacters) UsedCharacters.Add(character);
        }

        public static bool TryGetChartString(string maidataString, int i, out string chartString, out bool isLast)
        {
            chartString = string.Empty;
            isLast = false;

            if (string.IsNullOrEmpty(maidataString)) return false;

            var startToken = $"&inote_{i}=";
            var startIndex = maidataString.IndexOf(startToken, StringComparison.Ordinal);
            if (startIndex < 0) return false;

            var contentStart = startIndex + startToken.Length;
            var endIndex = -1;

            var eSearch = contentStart;
            while (true)
            {
                var eIndex = maidataString.IndexOf("E", eSearch, StringComparison.Ordinal);
                if (eIndex < 0) break;

                var nextPos = eIndex + 1;
                if (nextPos >= maidataString.Length || maidataString[nextPos] == '\r' || maidataString[nextPos] == '\n')
                {
                    endIndex = eIndex;
                    break;
                }

                eSearch = nextPos;
            }

            if (endIndex == -1)
            {
                var nextInotePos = FindNextHigherInote(maidataString, contentStart, i);
                if (nextInotePos != -1) endIndex = nextInotePos;
            }

            if (endIndex == -1)
            {
                chartString = maidataString.Substring(contentStart).Trim();
                isLast = true;
            }
            else
            {
                chartString = maidataString.Substring(contentStart, endIndex - contentStart).Trim();
                isLast = FindNextHigherInote(maidataString, endIndex, i) == -1;
            }

            return true;

            static int FindNextHigherInote(string str, int startSearch, int currentIdx)
            {
                var pos = startSearch;
                while ((pos = str.IndexOf("&inote_", pos, StringComparison.Ordinal)) != -1)
                {
                    var numStart = pos + "&inote_".Length;
                    var eqIdx = str.IndexOf('=', numStart);

                    if (eqIdx != -1)
                        if (int.TryParse(str.AsSpan(numStart, eqIdx - numStart), out var foundIdx) &&
                            foundIdx > currentIdx)
                            return pos;

                    pos++;
                }

                return -1;
            }
        }

        public void LoadSongCover()
        {
            if (_loadingCover)
                return;

            _loadingCover = true;
            using var image = File.Exists(_songCoverPath)
                ? Image.Load<Rgba32>(_songCoverPath)
                : new Image<Rgba32>(50, 50, new Rgba32(0.5f, 0.5f, 0.5f));

            SongCoverDecodedImage = new DecodedImage(image);

            CoverDataLoaded = true;
            _loadingCover = false;
        }

        public IEnumerator LoadSongClip()
        {
            if (_loadingSong)
                yield break;

            if (SongLoaded)
                yield break;

            _loadingSong = true;

            yield return AudioManager.GetInstance().LoadAudioClip(SongPath, clip => SongAudioClip = clip, true);

            SongLoaded = true;
            _loadingSong = false;
        }

        public void GenerateBlurredCover()
        {
            if (_generatingBlurredCover)
                return;

            if (BlurredSongCoverDecodedImage != null && BlurredSongCoverAsBackgroundDecodedImage != null)
                return;

            _generatingBlurredCover = true;

            using var image = File.Exists(_songCoverPath)
                ? Image.Load<Rgba32>(_songCoverPath)
                : new Image<Rgba32>(50, 50, new Rgba32(0.5f, 0.5f, 0.5f));
            using var transparentImage = new Image<Rgba32>(75, 75,
                new Rgba32(0, 0, 0, 0));

            image.Mutate(x => { x.Resize(50, 50); });

            var blurringLevel = SettingsPool.GetValue("graphics.blurred_cover");

            transparentImage.Mutate(x =>
            {
                x.DrawImage(image, new Point(
                    12, 12
                ), 1f);
                x.GaussianBlur(2);
            });

            BlurredSongCoverDecodedImage = new DecodedImage(transparentImage);

            image.Mutate(x => x.GaussianBlur(blurringLevel
                switch
                {
                    1 => 2,
                    2 => 5,
                    _ => 2
                }));

            BlurredSongCoverAsBackgroundDecodedImage = new DecodedImage(image);
            BlurredSongCoverGenerated = true;

            _generatingBlurredCover = false;
        }

        public class Chart
        {
            public string ChartString;
            public string Designer;
            public int DifficultyIndex;
            public string DifficultyString;
        }
    }
}