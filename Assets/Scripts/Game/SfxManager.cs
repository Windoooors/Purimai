using System;
using System.Collections.Generic;
using System.IO;
using UI.Settings;
using UI.Settings.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class SfxManager : MonoBehaviour
    {
        private static SfxManager _instance;

        private readonly Dictionary<string, BassHandler> _bassHandlers = new();

        private readonly Dictionary<string, float> _volumes = new();

        public readonly GameSoundNameData GameSoundNameData = new()
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
                { "good", new AudioSoundNameData("good_sound.wav") },
                { "ex", new AudioSoundNameData("ex_sound.wav") },
                { "break_slide_launch", new AudioSoundNameData("break_slide_launch_sound.wav") },
                { "touch_fireworks", new AudioSoundNameData("touch_fireworks.wav") },
                { "touch", new AudioSoundNameData("touch.wav") },
                { "touch_hold", new AudioSoundNameData("touch_hold.wav") }
            }
        };

        private bool _holdingSoundPausing;

        private bool _holdingSoundPlaying;

        public static SfxManager Instance => _instance ??= FindAnyObjectByType<SfxManager>();

        private void Awake()
        {
            _instance = this;

            //AdaptToSettings();

            SettingsManager.OnSettingsChanged += AdaptToSettings;
        }

        private void Start()
        {
            SceneManager.sceneLoaded += (_, _) =>
            {
                _holdingSoundPlaying = false;
                _holdingSoundPausing = false;

                StopTouchHoldSound();
            };
        }

        private void Update()
        {
            var sound = _bassHandlers["touch_hold"];

            if (_holdingSoundPlaying && !sound.IsPlaying && !_holdingSoundPausing)
                ResetAndPlayTouchHoldSound();
        }

        private void LoadAllSoundData()
        {
            foreach (var handler in _bassHandlers.Values)
                handler.Dispose();

            _bassHandlers.Clear();

            foreach (var audioSoundNameData in GameSoundNameData.audioSoundNameDataDict)
                LoadSingleSoundData(audioSoundNameData.Value, audioSoundNameData.Key);
        }

        public void AdaptToSettings()
        {
            UpdatePair("tap", SettingsPool.GetValue("volume.tap") / 10f);
            UpdatePair("break", SettingsPool.GetValue("volume.break") / 10f);
            UpdatePair("slide", SettingsPool.GetValue("volume.slide") / 10f);
            UpdatePair("cue", SettingsPool.GetValue("volume.cue_sound") / 10f);
            UpdatePair("ex", SettingsPool.GetValue("volume.ex") / 10f);
            UpdatePair("break_slide_launch", SettingsPool.GetValue("volume.break_slide_launch") / 10f);
            UpdatePair("break_slide_slide", SettingsPool.GetValue("volume.break_slide_slide") / 10f);
            UpdatePair("break_slide_judge", SettingsPool.GetValue("volume.break_slide_judge") / 10f);
            UpdatePair("touch", SettingsPool.GetValue("volume.touch") / 10f);
            UpdatePair("touch_fireworks", SettingsPool.GetValue("volume.touch_fireworks") / 10f);

            LoadAllSoundData();

            return;

            void UpdatePair(string key, float value)
            {
                if (!_volumes.TryAdd(key, value))
                    _volumes[key] = value;
            }
        }

        public void PlayBreakSlideLaunchingSound()
        {
            var sound = _bassHandlers["break_slide_launch"];
            sound.Volume = _volumes["break_slide_launch"];
            sound.PlayOneShot();
        }

        public void StopTouchHoldSound()
        {
            _holdingSoundPlaying = false;

            var sound = _bassHandlers["touch_hold"];
            sound.Stop();
        }

        public void ResetAndPlayTouchHoldSound()
        {
            var sound = _bassHandlers["touch_hold"];
            sound.Volume = _volumes["touch"];
            sound.Stop();
            sound.Play();

            _holdingSoundPlaying = true;
        }

        public void PlayTouchSound()
        {
            var sound = _bassHandlers["touch"];
            sound.Volume = _volumes["touch"];
            sound.PlayOneShot();
        }

        public void PauseTouchHoldSound()
        {
            var sound = _bassHandlers["touch_hold"];
            _holdingSoundPausing = true;

            sound.Pause();
        }

        public void UnpauseTouchHoldSound()
        {
            var sound = _bassHandlers["touch_hold"];
            _holdingSoundPausing = false;

            sound.Play();
        }

        public void PlayTouchFireworksSound()
        {
            var sound = _bassHandlers["touch_fireworks"];
            sound.Volume = _volumes["touch_fireworks"];
            sound.PlayOneShot();
        }

        public void PlayExSound()
        {
            var sound = _bassHandlers["ex"];
            sound.Volume = _volumes["ex"];
            sound.PlayOneShot();
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

        public void PlaySlideBreakPerfectSound()
        {
            var sound = _bassHandlers["break_extra"];

            if (_volumes["break_slide_judge"] == 0)
                return;

            sound.Volume = _volumes["break_slide_judge"];
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
            if (soundNameData.inStreamingAssets)
            {
                var path = Path.Combine(soundNameData.directoryRelativeToStreamingAssets,
                    soundNameData.fileNameRelativeToDirectory);

                var data = BetterStreamingAssets.ReadAllBytes(path);

                var bassHandler = new BassHandler(data);

                UpdatePair(dictKey, bassHandler);
            }
            else
            {
                var path = soundNameData.fileNameRelativeToDirectory;

                var bassHandler = new BassHandler(path);

                UpdatePair(dictKey, bassHandler);
            }

            return;

            void UpdatePair(string key, BassHandler value)
            {
                if (!_bassHandlers.TryAdd(key, value))
                {
                    _bassHandlers[key].Dispose();
                    _bassHandlers[key] = value;
                }
            }
        }
    }

    [Serializable]
    public class AudioSoundNameData
    {
        public string fileNameRelativeToDirectory;
        public string directoryRelativeToStreamingAssets = "default_sfx/game_sfx/";

        public bool inStreamingAssets = true;

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