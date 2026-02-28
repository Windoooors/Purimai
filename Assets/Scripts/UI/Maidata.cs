using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UI.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public static readonly HashSet<char> UsedCharacters = new();

        private readonly string _songCoverPath;
        private readonly string _songPath;

        public readonly string Artist;
        public readonly float Bpm;
        public readonly Chart[] Charts;
        public readonly float FirstNoteTime;
        public readonly string Genre;
        public readonly bool IsUtage;
        public readonly string MaidataDirectoryName;
        public readonly string PvPath;
        public readonly string Title;

        private bool _generatingBlurredCover;
        private bool _loadingCover;
        private bool _loadingSong;
        public DecodedImage BlurredSongCoverAsBackgroundDecodedImage;
        public DecodedImage BlurredSongCoverDecodedImage;
        public bool BlurredSongCoverGenerated;

        public DecodedImage BlurredSongCoverWithConstantRadiusDecodedImage;
        public bool CoverDataLoaded;
        public AudioClip SongAudioClip;
        public DecodedImage SongCoverDecodedImage;
        public bool SongLoaded;

        public Maidata(string maidataPath, string songPath, string pvPath, string songCoverPath)
        {
            MaidataDirectoryName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(maidataPath));
            _songPath = songPath;
            PvPath = pvPath;
            _songCoverPath = songCoverPath;

            var maidataString = File.ReadAllText(maidataPath);
            maidataString = new Regex(@"\|\|.*").Replace(maidataString, "");

            var titleMatch = TitleRegex.Match(maidataString);
            var artistMatch = ArtistRegex.Match(maidataString);
            var mainDesignerMatch = MainDesignerRegex.Match(maidataString);
            var genreMatch = GenreRegex.Match(maidataString);
            var firstNoteTimeMatch = FirstNoteTimeRegex.Match(maidataString);
            var bpmMatch = BpmRegex.Match(maidataString);

            Title = titleMatch.Success ? titleMatch.Groups[1].Value : "Unknown";
            Artist = artistMatch.Success ? artistMatch.Groups[1].Value : "Unknown";
            var mainChartDesigner = mainDesignerMatch.Success ? mainDesignerMatch.Groups[1].Value : "Unknown";
            Genre = genreMatch.Success ? genreMatch.Groups[1].Value : "Unknown";
            Bpm = bpmMatch.Success ? float.Parse(bpmMatch.Groups[1].Value) : -1;
            FirstNoteTime = firstNoteTimeMatch.Success ? float.Parse(firstNoteTimeMatch.Groups[1].Value) : 0;

            AddUsedCharacters(Title);
            AddUsedCharacters(Artist);
            AddUsedCharacters(mainChartDesigner);
            AddUsedCharacters(Genre);

            var chartList = new List<Chart>();

            var levelIndexArray = GetAllInoteIndexes(maidataString);

            if (levelIndexArray.Length == 0)
            {
                Charts = Array.Empty<Chart>();
                return;
            }

            foreach (var i in levelIndexArray)
            {
                var found = TryGetChartString(maidataString, i, out var chartString);

                if (!found) continue;

                if (!TryGetDesigner(maidataString, i, out var designer))
                    designer = mainChartDesigner;

                if (!TryGetLevel(maidataString, i, out var level))
                    level = "NaN";

                var chart = new Chart
                {
                    ChartString = chartString,
                    Designer = designer,
                    DifficultyString = level,
                    DifficultyIndex = i
                };

                if (UtageDifficultyRegex.IsMatch(level.ToLower()) || UtageTitleRegex.IsMatch(Title))
                    IsUtage = true;

                AddUsedCharacters(level);
                AddUsedCharacters(designer);

                chartList.Add(chart);
            }

            if (chartList.Count == 0)
            {
                Charts = Array.Empty<Chart>();
                return;
            }

            Charts = chartList.Where(x => x.ChartString.Trim() != "").Select(x => x).ToArray();

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

        public static int[] GetAllInoteIndexes(string maidataString)
        {
            if (string.IsNullOrEmpty(maidataString)) return new int[0];

            var indexList = new List<int>();
            var searchPos = 0;
            var tagStart = "&inote_";

            while (true)
            {
                var foundPos = maidataString.IndexOf(tagStart, searchPos);
                if (foundPos == -1) break;

                var numStart = foundPos + tagStart.Length;
                var equalPos = maidataString.IndexOf('=', numStart);

                if (equalPos != -1)
                {
                    var numStr = maidataString.Substring(numStart, equalPos - numStart);
                    if (int.TryParse(numStr, out var i)) indexList.Add(i);

                    searchPos = equalPos + 1;
                }
                else
                {
                    searchPos = numStart;
                }
            }

            return indexList.ToArray();
        }

        private bool TryGetLevel(string input, int index, out string level)
        {
            return TryGetField(input, $"&lv_{index}=", out level);
        }

        private bool TryGetDesigner(string input, int index, out string designer)
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

            value = lineEndIndex == -1
                ? input.Substring(valueStart)
                : input.Substring(valueStart, lineEndIndex - valueStart);

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
                Object.Destroy(SongAudioClip);

            SongAudioClip = null;
            SongLoaded = false;
        }

        public void UnloadResources()
        {
            if (SongAudioClip)
                Object.Destroy(SongAudioClip);

            BlurredSongCoverAsBackgroundDecodedImage?.Dispose();
            BlurredSongCoverWithConstantRadiusDecodedImage?.Dispose();

            BlurredSongCoverDecodedImage?.Dispose();

            SongCoverDecodedImage?.Dispose();

            BlurredSongCoverWithConstantRadiusDecodedImage = null;
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

        private static bool TryGetChartString(string maidataString, int i, out string chartString)
        {
            chartString = null;
            if (string.IsNullOrEmpty(maidataString)) return false;

            string startTag = $"&inote_{i}=";
            int startIndex = maidataString.IndexOf(startTag, StringComparison.InvariantCulture);
            if (startIndex == -1) return false;

            int contentStart = startIndex + startTag.Length;

            int nextTagPos = maidataString.Length;
            int searchPos = contentStart;
            while (true)
            {
                int tagPos = maidataString.IndexOf("&inote_", searchPos, StringComparison.InvariantCulture);
                if (tagPos == -1) break;

                int numStart = tagPos + 7; // "&inote_".Length
                int eqPos = maidataString.IndexOf('=', numStart);
                if (eqPos != -1)
                {
                    string numStr = maidataString.Substring(numStart, eqPos - numStart);
                    if (int.TryParse(numStr, out int k) && k > i)
                    {
                        nextTagPos = tagPos;
                        break;
                    }
                }

                searchPos = tagPos + 7;
            }
            
            int finalEndIndex = nextTagPos;
            int eSearchPos = contentStart;
            while (true)
            {
                int ePos = maidataString.IndexOf('E', eSearchPos);

                if (ePos == -1 || ePos >= nextTagPos) break;

                if (IsOnlyWhitespace(maidataString, ePos + 1, nextTagPos))
                {
                    finalEndIndex = ePos;
                    break;
                }

                eSearchPos = ePos + 1;
            }
            
            chartString = maidataString.Substring(contentStart, finalEndIndex - contentStart);
            return true;

            static bool IsOnlyWhitespace(string s, int start, int end)
            {
                for (int idx = start; idx < end; idx++)
                {
                    if (!char.IsWhiteSpace(s[idx])) return false;
                }

                return true;
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

        public IEnumerator LoadSongClip(bool streamed)
        {
            if (_loadingSong)
                yield break;

            if (SongLoaded)
                yield break;

            _loadingSong = true;

            yield return AudioManager.Instance.LoadAudioClip(_songPath, clip => SongAudioClip = clip, streamed);

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

            var blurringLevel = SettingsPool.GetValue("blurred_cover");

            transparentImage.Mutate(x =>
            {
                x.DrawImage(image, new Point(
                    12, 12
                ), 1f);
                x.GaussianBlur(2);
            });

            BlurredSongCoverDecodedImage = new DecodedImage(transparentImage);

            using var copiedImage = image.Clone();

            image.Mutate(x => x.GaussianBlur(blurringLevel
                switch
                {
                    1 => 2,
                    2 => 5,
                    _ => 2
                }));

            copiedImage.Mutate(x => x.GaussianBlur(5));

            BlurredSongCoverAsBackgroundDecodedImage = new DecodedImage(image);
            BlurredSongCoverWithConstantRadiusDecodedImage = new DecodedImage(copiedImage);
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