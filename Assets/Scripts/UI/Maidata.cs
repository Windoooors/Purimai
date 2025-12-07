using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FMOD;
using FMODUnity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace UI
{
    public class Maidata
    {
        private static readonly Regex TitleRegex = new("&title=(.*)");
        private static readonly Regex ArtistRegex = new("&artist=(.*)");
        private static readonly Regex MainDesignerRegex = new("&des=(.*)");
        private static readonly Regex GenreRegex = new("&genre=(.*)");
        private static readonly Regex FirstNoteTimeRegex = new(@"&first=(\d+.\d+|\d+)");
        private static readonly Regex BpmRegex = new(@"&wholebpm=(\d+.\d+|\d+)");

        private static readonly Regex UtageRegex =
            new(@"([0-9]+\+?\?|[\u4E00-\u9FFF]\s?[0-9]+\+?\??|utage\s?[0-9]+\+?\??)");

        private static readonly Regex[] LvRegexes =
        {
            new("&lv_0=(.*)"), new("&lv_1=(.*)"), new("&lv_2=(.*)"), new("&lv_3=(.*)"),
            new("&lv_4=(.*)"), new("&lv_5=(.*)"), new("&lv_6=(.*)")
        };

        private static readonly Regex[] DesRegexes =
        {
            new("&des_0=(.*)"), new("&des_1=(.*)"), new("&des_2=(.*)"), new("&des_3=(.*)"),
            new("&des_4=(.*)"), new("&des_5=(.*)"), new("&des_6=(.*)")
        };

        public static HashSet<char> UsedCharacters = new();

        private readonly string _songCoverPath;
        public readonly string Artist;

        public readonly float Bpm;
        public readonly string[] Charts;
        public readonly string[] Designers;

        public readonly string[] Difficulties;

        public readonly float FirstNoteTime;
        public readonly string Genre;

        public readonly bool IsUtage;
        public readonly string MaidataDirectoryName;
        public readonly string MainChartDesigner;
        public readonly string PvPath;
        public readonly string SongPath;

        public readonly string Title;
        public DecodedImage BlurredSongCoverAsBackgroundDecodedImage;
        public DecodedImage BlurredSongCoverDecodedImage;
        public bool BlurredSongCoverGenerated;
        public bool CoverDataLoaded;

        public DecodedImage SongCoverDecodedImage;

        public Sound SongFMODSound;
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

            Title = titleMatch.Success ? titleMatch.Groups[1].Value : "未知";
            Artist = artistMatch.Success ? artistMatch.Groups[1].Value : "未知";
            MainChartDesigner = mainDesignerMatch.Success ? mainDesignerMatch.Groups[1].Value : "未知";
            Genre = genreMatch.Success ? genreMatch.Groups[1].Value : "未知";
            Bpm = bpmMatch.Success ? float.Parse(bpmMatch.Groups[1].Value) : 0;
            FirstNoteTime = firstNoteTimeMatch.Success ? float.Parse(firstNoteTimeMatch.Groups[1].Value) : 0;

            AddUsedCharacters(Title);
            AddUsedCharacters(Artist);
            AddUsedCharacters(MainChartDesigner);
            AddUsedCharacters(Genre);

            var difficultyNameList = new List<string>();
            var designerList = new List<string>();
            var chartList = new List<string>();

            for (var i = 1; i <= 6; i++)
            {
                var levelRegex = LvRegexes[i];
                var designerRegex = DesRegexes[i];

                var levelMatch = levelRegex.IsMatch(maidata) ? levelRegex.Match(maidata).Groups[1].Value : "";
                var designerMatch = designerRegex.IsMatch(maidata)
                    ? designerRegex.Match(maidata).Groups[1].Value
                    : "";

                difficultyNameList.Add(levelMatch);
                designerList.Add(designerMatch);

                AddUsedCharacters(levelMatch);
                AddUsedCharacters(designerMatch);

                if (designerList[^1] == "")
                    designerList[^1] = MainChartDesigner;
                chartList.Add(GetChartString(maidata, i).Trim());
            }

            Difficulties = difficultyNameList.ToArray();
            Charts = chartList.ToArray();
            Designers = designerList.ToArray();

            IsUtage = true;

            var index = -1;
            foreach (var difficulty in Difficulties)
            {
                index++;
                if (Charts[index] == "")
                    continue;
                if (!UtageRegex.IsMatch(difficulty.ToLower()))
                    IsUtage = false;
            }
        }

        private static void AddUsedCharacters(string usedCharacters)
        {
            foreach (var character in usedCharacters) UsedCharacters.Add(character);
        }

        private static string GetChartString(string maidataString, int i)
        {
            var startToken = $"&inote_{i}=";
            var startIndex = maidataString.IndexOf(startToken, StringComparison.Ordinal);
            if (startIndex < 0)
                return string.Empty;

            var contentStart = startIndex + startToken.Length;
            var nextToken = $"&inote_{i + 1}=";
            var nextIndex = maidataString.IndexOf(nextToken, contentStart, StringComparison.Ordinal);

            if (nextIndex < 0)
                return maidataString.Substring(contentStart);

            return maidataString.Substring(contentStart, nextIndex - contentStart);
        }

        public void LoadSongCover()
        {
            using var image = File.Exists(_songCoverPath)
                ? Image.Load<Rgba32>(_songCoverPath)
                : new Image<Rgba32>(50, 50, new Rgba32(0.5f, 0.5f, 0.5f));

            SongCoverDecodedImage = new DecodedImage(image);

            CoverDataLoaded = true;
        }

        public void LoadSongClip()
        {
            var system = RuntimeManager.CoreSystem;

            var mode = MODE.DEFAULT |
                       MODE.NONBLOCKING |
                       MODE._2D;

            var result = system.createSound(SongPath, mode, out var sound);

            if (result != RESULT.OK) return;

            SongFMODSound = sound;

            SongLoaded = true;
        }

        public void GenerateBlurredCover()
        {
            if (BlurredSongCoverDecodedImage != null && BlurredSongCoverAsBackgroundDecodedImage != null)
                return;

            using var image = File.Exists(_songCoverPath)
                ? Image.Load<Rgba32>(_songCoverPath)
                : new Image<Rgba32>(50, 50, new Rgba32(0.5f, 0.5f, 0.5f));
            using var transparentImage = new Image<Rgba32>(75, 75,
                new Rgba32(0, 0, 0, 0));

            image.Mutate(x => { x.Resize(50, 50); });

            transparentImage.Mutate(x =>
            {
                x.DrawImage(image, new Point(
                    12, 12
                ), 1f);
                x.GaussianBlur(5);
            });

            BlurredSongCoverDecodedImage = new DecodedImage(transparentImage);

            image.Mutate(x => x.GaussianBlur(5));

            BlurredSongCoverAsBackgroundDecodedImage = new DecodedImage(image);
            BlurredSongCoverGenerated = true;
        }
    }
}