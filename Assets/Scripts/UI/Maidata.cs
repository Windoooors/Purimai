using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UnityEngine;
using UnityEngine.Networking;

namespace UI
{
        public class Maidata
        {
            private readonly string _songCoverPath;
            public readonly string Artist;

            public readonly float Bpm;
            public readonly string[] Charts;
            public readonly string[] Designers;

            public readonly string[] Difficulties;

            public readonly float FirstNoteTime;
            public readonly string Genre;

            public readonly bool IsUtage;
            public readonly string MainChartDesigner;
            public readonly string PvPath;
            public readonly string SongPath;

            public readonly string Title;
            public Sprite SongCover;
            public Sprite SongCoverBlurred;
            public Sprite SongCoverBlurredAsBackground;
            public AudioClip SongAudioClip;

            public bool SongLoaded;
            public bool BlurredSongCoverGenerated;

            public Maidata(string maidataPath, string songPath, string pvPath, string songCoverPath)
            {
                SongPath = songPath;
                PvPath = pvPath;
                _songCoverPath = songCoverPath;

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

                    var chartRegex = new Regex($@"&inote_{i}=((?s).*?)(?=E$|&inote_{i + 1}|\z)");

                    if (!chartRegex.IsMatch(maidata))
                    {
                        chartList.Add("");
                        continue;
                    }

                    if (designerList[^1] == "")
                        designerList[^1] = MainChartDesigner;
                    chartList.Add(chartRegex.Match(maidata).Groups[1].Value);
                }

                Difficulties = difficultyNameList.ToArray();
                Charts = chartList.ToArray();
                Designers = designerList.ToArray();

                var utageRegex = new Regex(@"([0-9]+\+?\?|[\u4E00-\u9FFF]\s?[0-9]+\+?\??|utage\s?[0-9]+\+?\??)");

                IsUtage = true;

                var index = -1;
                foreach (var difficulty in Difficulties)
                {
                    index++;
                    if (difficulty == string.Empty && Designers[index] == string.Empty)
                        continue;
                    if (!utageRegex.IsMatch(difficulty.ToLower()))
                        IsUtage = false;
                }
            }

            public IEnumerator LoadSprite()
            {
                var unityWebRequest = UnityWebRequestTexture.GetTexture(new Uri(_songCoverPath));

                SongCover = null;
                if (File.Exists(_songCoverPath))
                {
                    yield return unityWebRequest.SendWebRequest();

                    if (unityWebRequest.result == UnityWebRequest.Result.Success)
                    {
                        var texture = DownloadHandlerTexture.GetContent(unityWebRequest);
                        SongCover = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            Vector2.zero);
                    }

                    unityWebRequest.Dispose();
                }
            }

            public IEnumerator GenerateBlurredCover()
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(_songCoverPath);
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

                var memoryStream = new MemoryStream();
                transparentImage.SaveAsPng(memoryStream);
                var imageData = memoryStream.ToArray();
                var texture = new Texture2D(imageData.Length, imageData.Length, TextureFormat.RGBA32, false);
                texture.LoadImage(imageData);

                SongCoverBlurred = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                memoryStream.Dispose();

                memoryStream = new MemoryStream();
                image.Mutate(x => { x.GaussianBlur(5); });
                image.SaveAsPng(memoryStream);
                imageData = memoryStream.ToArray();
                texture = new Texture2D(imageData.Length, imageData.Length, TextureFormat.RGBA32, false);
                texture.LoadImage(imageData);
                texture.filterMode = FilterMode.Trilinear;

                SongCoverBlurredAsBackground =
                    Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                BlurredSongCoverGenerated = true;

                yield return null;
            }

            public IEnumerator LoadSong()
            {
                var audioType = Path.GetExtension(SongPath).ToLower() switch
                {
                    ".ogg" => AudioType.OGGVORBIS,
                    ".wav" => AudioType.WAV,
                    ".mp2" or ".mp3" => AudioType.MPEG,
                    _ => AudioType.UNKNOWN
                };

                if (audioType == AudioType.UNKNOWN)
                    throw new Exception("Unknown audio type: " + SongPath);

                var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(SongPath), audioType);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                    throw new Exception("Failed to load audio: " + request.error);

                SongAudioClip = DownloadHandlerAudioClip.GetContent(request);
                request.Dispose();

                SongLoaded = true;
            }
        }
}