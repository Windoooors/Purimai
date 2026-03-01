using System;
using System.Collections.Generic;
using System.IO;
using UI.Settings;
using UI.Settings.Managers;
using UnityEngine;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
using E7.Native;
#endif

namespace Game
{
    public class SfxManager : MonoBehaviour
    {
        private static SfxManager _instance;
        public UiSoundNameData uiSoundNameData;
        public GameSoundNameData gameSoundNameData;

        private readonly Dictionary<string, ClipHandler> _clipHandlers = new();
        private readonly Dictionary<string, float> _volumes = new();

        public ClipHandler CriticalSoundClip => _clipHandlers[gameSoundNameData.criticalSoundPath];
        public ClipHandler PreparatorySoundClip => _clipHandlers[gameSoundNameData.preparatoryBeatSoundPath];

        public static SfxManager Instance => _instance ?? FindAnyObjectByType<SfxManager>();

        private void Awake()
        {
            _instance = this;

            LoadAllSoundData();

            UpdateVolume();

            SettingsManager.OnSettingsChanged += UpdateVolume;
        }

        private void LoadAllSoundData()
        {
            var gameSoundPathData = gameSoundNameData.GetStreamingAssetsPrefixedPathData();

            LoadSingleSoundData(gameSoundPathData.perfectSoundPath, gameSoundNameData.perfectSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.greatSoundPath, gameSoundNameData.greatSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.goodSoundPath, gameSoundNameData.goodSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.breakExtraSoundPath, gameSoundNameData.breakExtraSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.breakPerfectSoundPath, gameSoundNameData.breakPerfectSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.breakGreatSoundPath, gameSoundNameData.breakGreatSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.slideSoundPath, gameSoundNameData.slideSoundPath, true);
            LoadSingleSoundData(gameSoundPathData.criticalSoundPath, gameSoundNameData.criticalSoundPath);
            LoadSingleSoundData(gameSoundPathData.preparatoryBeatSoundPath, gameSoundNameData.preparatoryBeatSoundPath);
        }

        private void UpdateVolume()
        {
            UpdatePair("tap", SettingsPool.GetValue("volume.tap") / 10f);
            UpdatePair("break", SettingsPool.GetValue("volume.break") / 10f);
            UpdatePair("slide", SettingsPool.GetValue("volume.slide") / 10f);

            return;

            void UpdatePair(string key, float value)
            {
                if (!_volumes.TryAdd(key, value))
                    _volumes[key] = value;
            }
        }

        public void PlaySlideSound()
        {
            var sound = _clipHandlers[gameSoundNameData.slideSoundPath];
            PlaySound(sound, _volumes["slide"]);
        }

        public void PlayBreakCriticalPerfectSound()
        {
            var sound = _clipHandlers[gameSoundNameData.breakPerfectSoundPath];
            PlaySound(sound, _volumes["break"]);
            sound = _clipHandlers[gameSoundNameData.breakExtraSoundPath];
            PlaySound(sound, _volumes["break"]);
        }

        public void PlayBreakPerfectSound()
        {
            var sound = _clipHandlers[gameSoundNameData.breakPerfectSoundPath];
            PlaySound(sound, _volumes["break"]);
        }

        public void PlayBreakGreatSound()
        {
            var sound = _clipHandlers[gameSoundNameData.breakGreatSoundPath];
            PlaySound(sound, _volumes["break"]);
        }

        public void PlayPerfectSound()
        {
            var sound = _clipHandlers[gameSoundNameData.perfectSoundPath];
            PlaySound(sound, _volumes["tap"]);
        }

        public void PlayGreatSound()
        {
            var sound = _clipHandlers[gameSoundNameData.greatSoundPath];
            PlaySound(sound, _volumes["tap"]);
        }

        public void PlayGoodSound()
        {
            var sound = _clipHandlers[gameSoundNameData.goodSoundPath];
            PlaySound(sound, _volumes["tap"]);
        }

        private void PlaySound(ClipHandler sound, float volume)
        {
            switch (sound)
            {
                case AudioClipHandler audioClipHandler:
                {
                    var succeed =
                        AudioManager.Instance.AudioSourcePool.TryGetAudioSourceHandler(out var handler,
                            audioClipHandler, true);

                    if (!succeed)
                        return;

                    var audioSourceHandler = (AudioSourceHandler)handler;
                    audioSourceHandler.SetClip(audioClipHandler);
                    audioSourceHandler.SetVolume(volume);
                    audioSourceHandler.Play();
                    break;
                }

                case NativePointerHandler pointerHandler:
                {
                    AudioManager.Instance.NativeSourcePool.TryGetAudioSourceHandler(out var handler, pointerHandler,
                        true);

                    var nativeHandler = (NativeAudioSourceHandler)handler;

                    nativeHandler.SetClip(pointerHandler);
                    nativeHandler.Play();
                    nativeHandler.SetVolume(volume);
                    break;
                }
            }
        }

        private void LoadSingleSoundData(string path, string soundName, bool useNative = false)
        {
            ClipHandler clipHandler;

            StartCoroutine(AudioManager.Instance.LoadAudioClip(path, clip =>
            {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
                if (useNative)
                    clipHandler = new NativePointerHandler(NativeAudio.Load(clip));
                else
                    clipHandler = new AudioClipHandler(clip);
#else
                clipHandler = new AudioClipHandler(clip);
#endif
                _clipHandlers.Add(soundName, clipHandler);
            }));
        }
    }

    [Serializable]
    public class UiSoundNameData
    {
        public string rollSoundPath;
        public string selectSoundPath;
        public string entrySoundPath;

        public UiSoundNameData GetStreamingAssetsPrefixedPathData(string prefix = "DefaultSFX/UserInterfaceSFX/")
        {
            return new UiSoundNameData
            {
                rollSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + rollSoundPath),
                selectSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + selectSoundPath),
                entrySoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + entrySoundPath)
            };
        }
    }

    [Serializable]
    public class GameSoundNameData
    {
        public string perfectSoundPath;
        public string greatSoundPath;
        public string goodSoundPath;
        public string breakExtraSoundPath;
        public string breakPerfectSoundPath;
        public string breakGreatSoundPath;
        public string slideSoundPath;
        public string criticalSoundPath;
        public string preparatoryBeatSoundPath;

        public GameSoundNameData GetStreamingAssetsPrefixedPathData(string prefix = "DefaultSFX/GameSFX/")
        {
            return new GameSoundNameData
            {
                perfectSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + perfectSoundPath),
                greatSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + greatSoundPath),
                goodSoundPath =
                    Path.Combine(Application.streamingAssetsPath, prefix + goodSoundPath),
                breakExtraSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + breakExtraSoundPath),
                breakPerfectSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + breakPerfectSoundPath),
                breakGreatSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + breakGreatSoundPath),
                slideSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + slideSoundPath),
                criticalSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + criticalSoundPath),
                preparatoryBeatSoundPath = Path.Combine(Application.streamingAssetsPath,
                    prefix + preparatoryBeatSoundPath)
            };
        }
    }
}