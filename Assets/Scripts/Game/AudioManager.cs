using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI;
using UI.Settings;
using UI.Settings.Managers;
using UnityEngine;
using UnityEngine.Networking;
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
using E7.Native;
#endif

namespace Game
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public AudioClip entrySound;
        public AudioClip rollSound;
        public AudioClip selectSound;

        public AudioClip criticalSound;
        public AudioClip preparatoryBeatSound;
#if !(UNITY_IOS && !UNITY_EDITOR)
        public AudioClip slideSound;
#endif
        private float _breakVolume = 1;
        private float _slideVolume = 1;

        private float _tapVolume = 1;

        public AudioSourcePool AudioSourcePool;

        public double DSPDurationInSeconds { private set; get; }

        private double _lastDspTime;
        private double _lastRealTime;

        public double EstimatedDspTime
        {
            get
            {
                var currentDsp = AudioSettings.dspTime;
                var currentReal = Time.realtimeSinceStartupAsDouble;

                if (currentDsp > _lastDspTime)
                {
                    _lastDspTime = currentDsp;
                    _lastRealTime = currentReal;
                }

                var elapsedSinceUpdate = currentReal - _lastRealTime;
                return _lastDspTime + elapsedSinceUpdate;
            }
        }

        private void Update()
        {
            var dummy = EstimatedDspTime;
        }

        private void Awake()
        {
            _instance = this;

            AudioSourcePool = new AudioSourcePool(32, gameObject);

            AudioSettings.GetDSPBufferSize(out var bufferSize, out _);
            var sampleRate = AudioSettings.outputSampleRate;

            DSPDurationInSeconds = bufferSize / (double)sampleRate;

            LoadAllSoundEffects(UIManager.Instance.gameSoundFileNameData, UIManager.Instance.uiSoundFileNameData);
        }

        public void OnApplicationQuit()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            NativeAudio.Dispose();

            _breakPerfectSound.Unload();
            _breakExtraSound.Unload();
            _goodSound.Unload();
            _greatSound.Unload();
            _perfectSound.Unload();
            _breakGreatSound.Unload();
#endif
#if (UNITY_IOS && !UNITY_EDITOR)
            _slideSound.Unload();
#endif
        }

        public static AudioManager Instance => _instance ?? FindAnyObjectByType<AudioManager>();

        public void PlayPerfectSound()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (_tapVolume == 0)
                return;

            if (!_tapNativeSource.IsValid)
            {
                _tapNativeSource = NativeAudio.GetNativeSource(0);
                _tapNativeSource.SetVolume(_tapVolume);
            }

            _tapNativeSource.Play(_perfectSound);
#endif
        }

        public void PlayGreatSound()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (_tapVolume == 0)
                return;

            if (!_tapNativeSource.IsValid)
            {
                _tapNativeSource = NativeAudio.GetNativeSource(0);
                _tapNativeSource.SetVolume(_tapVolume);
            }

            _tapNativeSource.Play(_greatSound);
#endif
        }

        public void PlayGoodSound()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (_tapVolume == 0)
                return;

            if (!_tapNativeSource.IsValid)
            {
                _tapNativeSource = NativeAudio.GetNativeSource(0);
                _tapNativeSource.SetVolume(_tapVolume);
            }

            _tapNativeSource.Play(_goodSound);
#endif
        }

        public void PlayBreakExtraScoreSound()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (_breakVolume == 0)
                return;

            if (!_breakExtraNativeSource.IsValid)
            {
                _breakExtraNativeSource = NativeAudio.GetNativeSource(1);
                _breakExtraNativeSource.SetVolume(_breakVolume);
            }

            _breakExtraNativeSource.Play(_breakExtraSound);
#endif
        }

        public void PlayBreakPerfectSound()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (_breakVolume == 0)
                return;

            if (!_breakNativeSource.IsValid)
            {
                _breakNativeSource = NativeAudio.GetNativeSource(2);
                _breakNativeSource.SetVolume(_breakVolume);
            }

            _breakNativeSource.Play(_breakPerfectSound);
#endif
        }

        public void PlayBreakGreatSound()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (_breakVolume == 0)
                return;

            if (!_breakNativeSource.IsValid)
            {
                _breakNativeSource = NativeAudio.GetNativeSource(2);
                _breakNativeSource.SetVolume(_breakVolume);
            }

            _breakNativeSource.Play(_breakGreatSound);
#endif
        }
#if !(UNITY_IOS && !UNITY_EDITOR)
        private AudioSourcePool.AudioSourceHandler _slideAudioSourceHandler;
#endif

        public void PlaySlideSound()
        {
#if !(UNITY_IOS && !UNITY_EDITOR)
            if (_slideVolume == 0)
                return;

            if (_slideAudioSourceHandler != null && _slideAudioSourceHandler.GetAudioClip() == slideSound)
                _slideAudioSourceHandler.Stop();

            var allocated = AudioSourcePool.TryGetAudioSourceHandler(out _slideAudioSourceHandler);

            if (!allocated)
                return;

            _slideAudioSourceHandler.SetClip(slideSound);
            _slideAudioSourceHandler.SetVolume(_slideVolume);
            _slideAudioSourceHandler.Stop();
            _slideAudioSourceHandler.Play();
#else
            if (_slideVolume == 0)
                return;

            if (!_slideNativeSource.IsValid)
            {
                _slideNativeSource = NativeAudio.GetNativeSource(3);
                _slideNativeSource.SetVolume(_slideVolume);
            }

            _slideNativeSource.Play(_slideSound);
#endif
        }

        private readonly float _selectVolume = 1;
        private AudioSourcePool.AudioSourceHandler _selectAudioSourceHandler;

        public void PlaySelectSound()
        {
            if (AudioSourcePool == null)
                return;

            if (_selectVolume == 0)
                return;

            if (_selectAudioSourceHandler != null && _selectAudioSourceHandler.GetAudioClip() == selectSound)
                _selectAudioSourceHandler.Stop();

            var allocated = AudioSourcePool.TryGetAudioSourceHandler(out _selectAudioSourceHandler);

            if (!allocated)
                return;

            _selectAudioSourceHandler.SetClip(selectSound);
            _selectAudioSourceHandler.SetVolume(_selectVolume);
            _selectAudioSourceHandler.Stop();
            _selectAudioSourceHandler.Play();
        }

        public IEnumerator LoadAudioClip(string path, Action<AudioClip> onComplete, bool streamed = false,
            bool compressed = false)
        {
            using var webRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(path), AudioType.UNKNOWN);
            ((DownloadHandlerAudioClip)webRequest.downloadHandler).streamAudio = streamed;
            if (!streamed)
                ((DownloadHandlerAudioClip)webRequest.downloadHandler).compressed = compressed;
            yield return webRequest.SendWebRequest();

            var clip = DownloadHandlerAudioClip.GetContent(webRequest);

            onComplete?.Invoke(clip);
        }

        public void LoadAllSoundEffects(GameSoundNameData gameSoundEffectFileNameData, UiSoundNameData uiSoundNameData)
        {
            _tapVolume = SettingsPool.GetValue("volume.tap") / 10f;
            _breakVolume = SettingsPool.GetValue("volume.break") / 10f;
            _slideVolume = SettingsPool.GetValue("volume.slide") / 10f;
            SettingsManager.OnSettingsChanged += () =>
            {
                _tapVolume = SettingsPool.GetValue("volume.tap") / 10f;
                _breakVolume = SettingsPool.GetValue("volume.break") / 10f;
                _slideVolume = SettingsPool.GetValue("volume.slide") / 10f;
            };

            var gameSoundPathData = gameSoundEffectFileNameData.GetStreamingAssetsPrefixedPathData();
            var uiSoundPathData = uiSoundNameData.GetStreamingAssetsPrefixedPathData();

            StartCoroutine(LoadAudioClip(uiSoundPathData.entrySoundPath, clip => { entrySound = clip; }));
            StartCoroutine(LoadAudioClip(uiSoundPathData.rollSoundPath,
                clip => { rollSound = clip; }));
            StartCoroutine(LoadAudioClip(uiSoundPathData.selectSoundPath,
                clip => { selectSound = clip; }));

            StartCoroutine(LoadAudioClip(gameSoundPathData.criticalSoundPath, clip => { criticalSound = clip; }));
            StartCoroutine(LoadAudioClip(gameSoundPathData.preparatoryBeatSoundPath,
                clip => { preparatoryBeatSound = clip; }));

#if !(UNITY_IOS && !UNITY_EDITOR)
            StartCoroutine(LoadAudioClip(gameSoundPathData.slideSoundPath,
                clip => { slideSound = clip; }));
#endif

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            NativeAudio.Initialize();

            StartCoroutine(LoadAudioClip(gameSoundPathData.perfectSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _perfectSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(gameSoundPathData.breakExtraSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _breakExtraSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(gameSoundPathData.breakPerfectSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _breakPerfectSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(gameSoundPathData.breakGreatSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _breakGreatSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(gameSoundPathData.greatSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _greatSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(gameSoundPathData.goodSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _goodSound = NativeAudio.Load(clip);
                }));
#endif
#if (UNITY_IOS && !UNITY_EDITOR)
            StartCoroutine(LoadAudioClip(gameSoundPathData.slideSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _slideSound = NativeAudio.Load(clip);
                }));
#endif
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
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
        private static NativeAudioPointer _perfectSound;
        private static NativeAudioPointer _greatSound;
        private static NativeAudioPointer _goodSound;
        private static NativeAudioPointer _breakExtraSound;
        private static NativeAudioPointer _breakPerfectSound;
        private static NativeAudioPointer _breakGreatSound;

        private static NativeSource _tapNativeSource;
        private static NativeSource _breakNativeSource;
        private static NativeSource _breakExtraNativeSource;
#endif

#if (UNITY_IOS && !UNITY_EDITOR)
        private static NativeSource _slideNativeSource;
        private static NativeAudioPointer _slideSound;
#endif
    }

    public class AudioSourcePool
    {
        public readonly List<AudioSourceHandler> Pool = new();

        public readonly int Size;

        public AudioSourcePool(int size, GameObject parentObject)
        {
            Size = size;

            for (var i = 0; i < size; i++)
            {
                var source = parentObject.AddComponent<AudioSource>();
                var handler = new AudioSourceHandler(source);

                Pool.Add(handler);
            }
        }

        public int GetOccupiedCount()
        {
            var count = 0;

            foreach (var handler in Pool)
                if (!handler.IsFree)
                    count++;

            return count;
        }

        public bool TryGetAudioSourceHandler(out AudioSourceHandler audioSourceHandler)
        {
            foreach (var handler in Pool)
                if (handler.IsFree)
                {
                    audioSourceHandler = handler;
                    return true;
                }

            audioSourceHandler = null;
            return false;
        }

        public void Clear()
        {
            Pool.ForEach(x =>
            {
                x.ScheduledStartTime = -1;
                x.Stop();
            });
        }

        public void PauseAll()
        {
            Pool.ForEach(x =>
            {
                if (!x.IsFree)
                    x.Pause();
            });
        }

        public void PlayAll()
        {
            Pool.ForEach(x =>
            {
                if (!x.IsFree)
                    x.Play();
            });
        }

        public class AudioSourceHandler
        {
            private readonly AudioSource _audioSource;

            private bool _paused;

            public double ScheduledStartTime = -1;

            public AudioSourceHandler(AudioSource audioSource)
            {
                _audioSource = audioSource;
            }

            public bool IsFree
            {
                get
                {
                    if (_audioSource.isPlaying || _paused) return false;

                    if (ScheduledStartTime >= 0 && AudioManager.Instance.EstimatedDspTime < ScheduledStartTime)
                        return false;

                    if (ScheduledStartTime >= 0 && AudioManager.Instance.EstimatedDspTime >= ScheduledStartTime)
                        ScheduledStartTime = -1;

                    _audioSource.Stop();

                    return true;
                }
            }

            public void SetPosition(float position)
            {
                _audioSource.time = position;
            }

            public float GetPosition()
            {
                return _audioSource.time;
            }

            public int GetTimeSamples()
            {
                return _audioSource.timeSamples;
            }

            public void SetTimeSamples(int timeSamples)
            {
                _audioSource.timeSamples = timeSamples;
            }

            public AudioClip GetAudioClip()
            {
                return _audioSource.clip;
            }

            public void Pause()
            {
                _audioSource.Pause();
                _paused = true;
            }

            public void Resume()
            {
                _audioSource.UnPause();
                _paused = false;
            }

            public void SetVolume(float volume)
            {
                _audioSource.volume = volume;
            }

            public void SetClip(AudioClip clip)
            {
                _audioSource.clip = clip;
            }

            public bool IsPlaying()
            {
                return _audioSource.isPlaying;
            }

            public void Play()
            {
                _audioSource.Play();
                _paused = false;
            }

            public void Stop()
            {
                _audioSource.Stop();
                _paused = false;
            }

            public void PlayScheduled(double time)
            {
                _audioSource.PlayScheduled(time);
            }
        }
    }
}