using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.Settings;
using UI.Settings.Managers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game
{
    public static class FileDownloader
    {
        public static IEnumerator DownloadFile(string fromFileNameRelativeToStreamingAssets, string toFileName,
            Action callback)
        {
            var directory = Path.GetDirectoryName(toFileName);

            if (directory == null) yield break;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(toFileName))
            {
                callback();
                yield break;
            }

            var uri = new Uri(Path.Combine(Application.streamingAssetsPath, fromFileNameRelativeToStreamingAssets));

            var request = UnityWebRequest.Get(uri);

            yield return request.SendWebRequest();

            if (request.downloadHandler.data != null)
                File.WriteAllBytes(toFileName, request.downloadHandler.data);

            request.Dispose();

            callback();
        }
    }

    public class SfxManager : MonoBehaviour
    {
        private static SfxManager _instance;

        private readonly Dictionary<string, BassHandler> _bassHandlers = new();

        private readonly GameSoundNameData _gameSoundNameData = new()
        {
            audioSoundNameDataDict = new Dictionary<string, AudioSoundNameData>
            {
                { "cue", new AudioSoundNameData("cue_sound.wav") },
                { "slide", new AudioSoundNameData("slide_sound.wav") },
                { "break_extra", new AudioSoundNameData("break_extra_sound.wav") },
                { "break_perfect", new AudioSoundNameData("break_perfect_sound.wav") },
                { "break_great", new AudioSoundNameData("break_great_sound.wav") },
                { "perfect", new AudioSoundNameData("perfect_sound.wav") },
                { "great", new AudioSoundNameData("great_sound.wav") },
                { "good", new AudioSoundNameData("good_sound.wav") }
            }
        };

        private readonly Dictionary<string, float> _volumes = new();

        public static SfxManager Instance => _instance ?? FindAnyObjectByType<SfxManager>();

        private void Awake()
        {
            _instance = this;

            AdaptToSettings();

            SettingsManager.OnSettingsChanged += AdaptToSettings;
        }

        private void LoadAllSoundData()
        {
            foreach (var handler in _bassHandlers.Values)
                handler.Dispose();

            _bassHandlers.Clear();

            foreach (var audioSoundNameData in _gameSoundNameData.audioSoundNameDataDict)
                LoadSingleSoundData(audioSoundNameData.Value, audioSoundNameData.Key);
        }

        private void AdaptToSettings()
        {
            UpdatePair("tap", SettingsPool.GetValue("volume.tap") / 10f);
            UpdatePair("break", SettingsPool.GetValue("volume.break") / 10f);
            UpdatePair("slide", SettingsPool.GetValue("volume.slide") / 10f);
            UpdatePair("cue", SettingsPool.GetValue("volume.cue_sound") / 10f);

            LoadAllSoundData();

            return;

            void UpdatePair(string key, float value)
            {
                if (!_volumes.TryAdd(key, value))
                    _volumes[key] = value;
            }
        }

        public void PlaySlideSound()
        {
            var sound = _bassHandlers["slide"];
            sound.Volume = _volumes["slide"];
            sound.PlayOneShot();
        }

        public void PlayCueSound()
        {
            var sound = _bassHandlers["cue"];
            sound.Volume = _volumes["cue"];
            sound.PlayOneShot();
        }

        public void PlayBreakCriticalPerfectSound()
        {
            var sound = _bassHandlers["break_extra"];
            sound.Volume = _volumes["break"];
            sound.PlayOneShot();
            sound = _bassHandlers["break_perfect"];
            sound.Volume = _volumes["break"];
            sound.PlayOneShot();
        }

        public void PlayBreakPerfectSound()
        {
            var sound = _bassHandlers["break_perfect"];
            sound.Volume = _volumes["break"];
            sound.PlayOneShot();
        }

        public void PlayBreakGreatSound()
        {
            var sound = _bassHandlers["break_great"];
            sound.Volume = _volumes["break"];
            sound.PlayOneShot();
        }

        public void PlayPerfectSound()
        {
            var sound = _bassHandlers["perfect"];
            sound.Volume = _volumes["tap"];
            sound.PlayOneShot();
        }

        public void PlayGreatSound()
        {
            var sound = _bassHandlers["great"];
            sound.Volume = _volumes["tap"];
            sound.PlayOneShot();
        }

        public void PlayGoodSound()
        {
            var sound = _bassHandlers["good"];
            sound.Volume = _volumes["tap"];
            sound.PlayOneShot();
        }

        private void LoadSingleSoundData(AudioSoundNameData soundNameData, string dictKey)
        {
            var path = Path.Combine(soundNameData.directoryRelativeToStreamingAssets,
                soundNameData.fileNameRelativeToDirectory);
            var toPath = Path.Combine(Application.persistentDataPath, path);

            StartCoroutine(FileDownloader.DownloadFile(path, toPath, () =>
            {
                var bassHandler = new BassHandler(toPath);

                _bassHandlers.Add(dictKey, bassHandler);
            }));
        }
    }

    [Serializable]
    public class AudioSoundNameData
    {
        public string fileNameRelativeToDirectory;
        public string directoryRelativeToStreamingAssets = "DefaultSFX/GameSFX/";

        public AudioSoundNameData(string fileNameRelativeToDirectory)
        {
            this.fileNameRelativeToDirectory = fileNameRelativeToDirectory;
        }
    }

    [Serializable]
    public class GameSoundNameData
    {
        public Dictionary<string, AudioSoundNameData> audioSoundNameDataDict;
    }
}