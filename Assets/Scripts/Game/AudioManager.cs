using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.GameSettings;
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

        public AudioClip criticalSound;
        public AudioClip preparatoryBeatSound;
#if !(UNITY_IOS && !UNITY_EDITOR)
        public AudioClip slideSound;
#endif
        private float _breakVolume = 1;
        private float _slideVolume = 1;

        private float _tapVolume = 1;

        public AudioSourcePool AudioSourcePool;

        private void Awake()
        {
            _instance = this;

            AudioSourcePool = new AudioSourcePool(32, gameObject);
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

        public static AudioManager GetInstance()
        {
            if (_instance == null)
                return FindAnyObjectByType<AudioManager>();
            return _instance;
        }

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

        public void LoadAllSoundEffects(SoundNameData soundEffectFileNameData)
        {
            _tapVolume = SettingsPool.GetValue("game.volume.tap") / 10f;
            _breakVolume = SettingsPool.GetValue("game.volume.break") / 10f;
            _slideVolume = SettingsPool.GetValue("game.volume.slide") / 10f;
            SettingsController.OnSettingsChanged += (_, _) =>
            {
                _tapVolume = SettingsPool.GetValue("game.volume.tap") / 10f;
                _breakVolume = SettingsPool.GetValue("game.volume.break") / 10f;
                _slideVolume = SettingsPool.GetValue("game.volume.slide") / 10f;
            };

            var soundPathData = soundEffectFileNameData.GetStreamingAssetsPrefixedPathData();

            StartCoroutine(LoadAudioClip(soundPathData.criticalSoundPath, clip => { criticalSound = clip; }));
            StartCoroutine(LoadAudioClip(soundPathData.preparatoryBeatSoundPath,
                clip => { preparatoryBeatSound = clip; }));

#if !(UNITY_IOS && !UNITY_EDITOR)
            StartCoroutine(LoadAudioClip(soundPathData.slideSoundPath,
                clip => { slideSound = clip; }));
#endif

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            NativeAudio.Initialize();

            StartCoroutine(LoadAudioClip(soundPathData.perfectSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _perfectSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(soundPathData.breakExtraSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _breakExtraSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(soundPathData.breakPerfectSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _breakPerfectSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(soundPathData.breakGreatSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _breakGreatSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(soundPathData.greatSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _greatSound = NativeAudio.Load(clip);
                }));

            StartCoroutine(LoadAudioClip(soundPathData.goodSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _goodSound = NativeAudio.Load(clip);
                }));
#endif
#if (UNITY_IOS && !UNITY_EDITOR)
            StartCoroutine(LoadAudioClip(soundPathData.slideSoundPath,
                clip =>
                {
                    clip.LoadAudioData();
                    _slideSound = NativeAudio.Load(clip);
                }));
#endif
        }

        [Serializable]
        public class SoundNameData
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

            public SoundNameData GetStreamingAssetsPrefixedPathData()
            {
                return new SoundNameData
                {
                    perfectSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + perfectSoundPath),
                    greatSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + greatSoundPath),
                    goodSoundPath =
                        Path.Combine(Application.streamingAssetsPath, "DefaultSFX/GameSFX/" + goodSoundPath),
                    breakExtraSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + breakExtraSoundPath),
                    breakPerfectSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + breakPerfectSoundPath),
                    breakGreatSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + breakGreatSoundPath),
                    slideSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + slideSoundPath),
                    criticalSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + criticalSoundPath),
                    preparatoryBeatSoundPath = Path.Combine(Application.streamingAssetsPath,
                        "DefaultSFX/GameSFX/" + preparatoryBeatSoundPath)
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
        private readonly List<AudioSourceHandler> _pool = new();

        public readonly int Size;

        public AudioSourcePool(int size, GameObject parentObject)
        {
            Size = size;

            for (var i = 0; i < size; i++)
            {
                var source = parentObject.AddComponent<AudioSource>();
                var handler = new AudioSourceHandler(source);

                _pool.Add(handler);
            }
        }

        public int GetOccupiedCount()
        {
            var count = 0;

            foreach (var handler in _pool)
                if (!handler.IsFree)
                    count++;

            return count;
        }

        public bool TryGetAudioSourceHandler(out AudioSourceHandler audioSourceHandler)
        {
            foreach (var handler in _pool)
                if (handler.IsFree)
                {
                    audioSourceHandler = handler;
                    return true;
                }

            audioSourceHandler = null;
            return false;
        }

        public class AudioSourceHandler
        {
            private readonly AudioSource _audioSource;

            private bool _paused;

            public double ScheduledStartTime = -1;

            public AudioClip GetAudioClip()
            {
                return _audioSource.clip;
            }

            public AudioSourceHandler(AudioSource audioSource)
            {
                _audioSource = audioSource;
            }

            public bool IsFree
            {
                get
                {
                    if (_audioSource.isPlaying || _paused) return false;

                    if (ScheduledStartTime >= 0 && AudioSettings.dspTime < ScheduledStartTime) return false;

                    if (ScheduledStartTime >= 0 && AudioSettings.dspTime >= ScheduledStartTime) ScheduledStartTime = -1;

                    _audioSource.Stop();

                    return true;
                }
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
            }

            public void Stop()
            {
                _audioSource.Stop();
            }

            public void PlayScheduled(double delay)
            {
                _audioSource.PlayScheduled(delay);
            }
        }
    }
}