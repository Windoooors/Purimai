using System;
using System.Collections;
using System.Collections.Generic;
using E7.Native;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Game
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        private double _lastDspTime;
        private double _lastRealTime;

        public AudioSourcePool AudioSourcePool;
        public AudioSourcePool NativeSourcePool;

        public double DSPDurationInSeconds { private set; get; }

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

        public static AudioManager Instance => _instance ?? FindAnyObjectByType<AudioManager>();

        private void Awake()
        {
            _instance = this;

            AudioSourcePool = new AudioSourcePool(32, gameObject);
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            NativeSourcePool = new AudioSourcePool();
#endif

            AudioSettings.GetDSPBufferSize(out var bufferSize, out _);
            var sampleRate = AudioSettings.outputSampleRate;

            DSPDurationInSeconds = bufferSize / (double)sampleRate;
        }

        private void Update()
        {
            var dummy = EstimatedDspTime;
        }

        public void OnApplicationQuit()
        {
            NativeAudio.Dispose();
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
    }

    public interface IAudioSourceHandler
    {
        public int SerialCount { get; }

        public bool IsFree { get; }

        public void Play();
        public void Pause();
        public void Stop();

        public void Clear();

        public ClipHandler GetClip();

        public void SerialTick();

        public void SetVolume(float volume);
    }

    public class AudioSourcePool
    {
        public readonly List<IAudioSourceHandler> Pool = new();

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

        public AudioSourcePool()
        {
            NativeAudio.Initialize();

            var sourceCount = NativeAudio.GetNativeSourceCount();

            for (var i = 0; i < sourceCount; i++) Pool.Add(new NativeAudioSourceHandler(i));
        }

        public int GetOccupiedCount()
        {
            var count = 0;

            foreach (var handler in Pool)
                if (!handler.IsFree)
                    count++;

            return count;
        }

        public bool TryGetAudioSourceHandler(out IAudioSourceHandler audioSourceHandler, ClipHandler clipHandler = null,
            bool forced = false)
        {
            if (clipHandler != null)
                foreach (var handler in Pool)
                    if (clipHandler == handler.GetClip())
                    {
                        audioSourceHandler = handler;

                        audioSourceHandler.SerialTick();
                        audioSourceHandler.Clear();
                        return true;
                    }

            foreach (var handler in Pool)
                if (handler.IsFree)
                {
                    audioSourceHandler = handler;

                    audioSourceHandler.SerialTick();
                    return true;
                }

            if (forced)
            {
                Pool.Sort((x, y) => x.SerialCount.CompareTo(y.SerialCount));

                audioSourceHandler = Pool[0];
                audioSourceHandler.SerialTick();
                audioSourceHandler.Clear();

                return true;
            }

            audioSourceHandler = null;
            return false;
        }

        public void Clear()
        {
            Pool.ForEach(x => { x.Clear(); });
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
    }

    public abstract class ClipHandler : IDisposable
    {
        public abstract void Dispose();
    }

    public class AudioClipHandler : ClipHandler
    {
        public AudioClip clip;

        public AudioClipHandler(AudioClip clip)
        {
            this.clip = clip;
        }

        public override void Dispose()
        {
            Object.Destroy(clip);
            clip = null;
        }
    }

    public class NativePointerHandler : ClipHandler
    {
        public NativeAudioPointer clipPointer;

        public NativePointerHandler(NativeAudioPointer clipPointer)
        {
            this.clipPointer = clipPointer;
        }

        public override void Dispose()
        {
            clipPointer.Unload();
            clipPointer = null;
        }
    }

    public class NativeAudioSourceHandler : IAudioSourceHandler
    {
        private static int _globalSerialCount;

        private NativePointerHandler _clipHandler;
        private NativeSource _nativeSource;

        public NativeAudioSourceHandler(int nativeSourceIndex)
        {
            _nativeSource = NativeAudio.GetNativeSource(nativeSourceIndex);
        }

        public void SetVolume(float volume)
        {
            _nativeSource.SetVolume(volume);
        }

        public void SerialTick()
        {
            _globalSerialCount++;
            SerialCount = _globalSerialCount;
        }

        public int SerialCount { get; private set; }

        public bool IsFree => _nativeSource.GetPlaybackTime() == 0;

        public void Stop()
        {
            _nativeSource.Stop();
        }

        public ClipHandler GetClip()
        {
            return _clipHandler;
        }

        public void Play()
        {
            _nativeSource.Play(_clipHandler.clipPointer);
        }

        public void Pause()
        {
            _nativeSource.Pause();
        }

        public void Clear()
        {
            _nativeSource.Stop();
            _clipHandler = null;
        }

        public void SetClip(ClipHandler clipHandler)
        {
            _clipHandler = (NativePointerHandler)clipHandler;
        }
    }

    public class AudioSourceHandler : IAudioSourceHandler
    {
        private static int _globalSerialCount;
        private readonly AudioSource _audioSource;

        private bool _paused;

        private ClipHandler clipHandler;

        public double ScheduledStartTime = -1;

        public AudioSourceHandler(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }

        public void Clear()
        {
            ScheduledStartTime = -1;
            clipHandler = null;
            Stop();
        }

        public void SerialTick()
        {
            _globalSerialCount++;
            SerialCount = _globalSerialCount;
        }

        public int SerialCount { get; private set; }

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

        public void Pause()
        {
            _audioSource.Pause();
            _paused = true;
        }

        public void SetVolume(float volume)
        {
            _audioSource.volume = volume;
        }

        public ClipHandler GetClip()
        {
            return clipHandler;
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

        public void SetPosition(float position)
        {
            _audioSource.time = position;
        }

        public float GetPosition()
        {
            return _audioSource.time;
        }

        public AudioClip GetAudioClip()
        {
            return _audioSource.clip;
        }

        public void Resume()
        {
            _audioSource.UnPause();
            _paused = false;
        }

        public void SetClip(ClipHandler clip)
        {
            _audioSource.clip = ((AudioClipHandler)clip).clip;
        }

        public bool IsPlaying()
        {
            return _audioSource.isPlaying;
        }

        public void PlayScheduled(double time)
        {
            _audioSource.PlayScheduled(time);
        }
    }
}