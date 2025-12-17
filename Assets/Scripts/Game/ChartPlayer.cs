using System;
using System.Collections.Generic;
using FMOD;
using FMODUnity;
using Game;
using Game.Notes;
using LitMotion;
using UI;
using UI.GameSettings;
using UI.Result;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using EditorScript;
#endif

namespace Game
{
    public class ChartPlayer : MonoBehaviour
    {
        public static ChartPlayer Instance;

        public RawImage backgroundImage;

        public float flowSpeed;
        public float slideAppearanceDelay;

        public int slideJudgeDisplayAnimationDuration = 600;
        public int slideConcealDelay = 33;

        public int judgeDelay;

        public SpriteRenderer judgeCircleSpriteRenderer;
        public SpriteRenderer judgeCircleGlowSpriteRenderer;

        public Color[] judgeCircleColors;

        [HideInInspector] public int slideAppearanceDeltaTime;
        [HideInInspector] public int slideFadeInDuration;

        public Animator[] holdRippleAnimators;
        public Animator judgeCircleGlowAnimator;

        public AnimationClip judgeDisplayAnimationClip;

        public JudgeSettings tapJudgeSettings;
        public JudgeSettings slideJudgeSettings;
        public JudgeSettings holdTailJudgeSettings;
        
        [SerializeField] private float _time;

        public bool isPlaying;

        private readonly float _generalPlaybackDelayInSeconds = 3f;
        private ChannelGroup _channelGroup;

        private bool _chartHasVideo;

        private ChannelPool _criticalSoundChannelPool;

        private int _criticalSoundIndex;

        private float _criticalSoundVolume;
        
        private ulong _dspClockWhenPlaybackStarts;
        [SerializeField] private float _dspTime;
        private int _frequency;

        private Channel _songChannel;
        private float _songVolume;
        private VideoPlayer _videoPlayer;
        private RenderTexture _videoTexture;

        public Sound SongClip;

        private bool _paused;
        private float _songLength;

        public void Awake()
        {
            Instance = this;

            judgeDelay = SettingsPool.GetValue("game.judge_delay");
            flowSpeed = SettingsPool.GetValue("game.flow_speed") * 0.25f + 1;
            slideAppearanceDelay = (SettingsPool.GetValue("game.slide_delay") - 10) / 10f;

            slideJudgeDisplayAnimationDuration = (int)(judgeDisplayAnimationClip.length * 1000);

            slideAppearanceDeltaTime = -(int)(2400 / flowSpeed * (1 - slideAppearanceDelay));
            slideFadeInDuration = -slideAppearanceDeltaTime > 200 ? 200 : -slideAppearanceDeltaTime;

            _criticalSoundChannelPool = new ChannelPool(24);

            SceneManager.sceneUnloaded += _ => { Destroy(_videoTexture); };

            _time = -_generalPlaybackDelayInSeconds;

            _criticalSoundVolume = SettingsPool.GetValue("game.volume.critical_sound") / 10f;
            _songVolume = SettingsPool.GetValue("game.volume.song") / 10f;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Pause();
            else
                Resume();
        }

        private void Pause()
        {
            _channelGroup.setPaused(true);
            _paused = true;
        }

        private void Resume()
        {
            _needSync = true;
            _paused = false;
        }

        private bool _needSync;

        private void Update()
        {
            if (isPlaying)
            {
                _channelGroup.getDSPClock(out _, out var currentDspClock);

                _dspTime = (float)(currentDspClock - _dspClockWhenPlaybackStarts) / _frequency -
                           _generalPlaybackDelayInSeconds;
            }
            
            if (isPlaying && !_paused)
            {
                if (_needSync)
                {
                    _needSync = false;

                    _songChannel.setPaused(false);

                    _time = _dspTime;
                }
                
                _time += Time.deltaTime;
                
                SetCriticalSoundChannel();
                
                if (_time > math.max(NoteGenerator.Instance.endingTime / 1000f + 0.5f, _songLength))
                {
                    isPlaying = false;
                    LMotion.Create(_songVolume, 0, 0.5f).WithOnComplete(OnPlayCompleted)
                        .Bind(x =>
                        {
                            if (_songChannel.hasHandle())
                                _songChannel.setVolume(x);
                        });
                }
            }
        }

        public void LoadVideo(string path)
        {
            if (path == "")
                return;

            _chartHasVideo = true;

            _videoPlayer = backgroundImage.gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.url = path;

            _videoPlayer.prepareCompleted += vp =>
            {
                _videoTexture = new RenderTexture((int)vp.width, (int)vp.height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "VideoRT",
                    useMipMap = false,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                _videoTexture.Create();

                vp.targetTexture = _videoTexture;
                backgroundImage.texture = _videoTexture;

                var fitter = backgroundImage.GetComponent<AspectRatioFitter>();
                if (fitter == null)
                    fitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();

                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)vp.width / vp.height;
            };

            _videoPlayer.Prepare();
        }

        public float GetTime()
        {
            return _time * 1000;
        }

        public void InitializeCircleColor(int index, bool isUtage)
        {
            judgeCircleSpriteRenderer.color = judgeCircleColors[isUtage ? 5 : index == 5 ? 4 : index];
            judgeCircleGlowSpriteRenderer.color = judgeCircleColors[isUtage ? 5 : index == 5 ? 4 : index];
        }

        private void OnPlayCompleted()
        {
            _songChannel.stop();
            
            UIManager.GetInstance().EnableUI();
            ResultController.GetInstance().ShowResult();
        }

        public void Play()
        {
            LMotion.Create(_generalPlaybackDelayInSeconds * 1000f, 0, _generalPlaybackDelayInSeconds)
                .WithEase(Ease.Linear).WithOnComplete(() =>
                {
                    if (_chartHasVideo)
                    {
                        _videoPlayer.Play();
                    }
                }).Bind(_ => { });

            isPlaying = true;

            PlaySound();
        }

        private void PlaySound()
        {
            var system = SoundEffectManager.System;

            var masterGroup = SoundEffectManager.GetChannelGroup();

            var result = system.getSoftwareFormat(
                out var frequency,
                out _,
                out _
            );

            masterGroup.getDSPClock(out _, out _dspClockWhenPlaybackStarts);

            var songStartOffset = (ulong)(frequency * _generalPlaybackDelayInSeconds);

            system.playSound(SongClip, masterGroup, true, out _songChannel);
            _songChannel.setDelay(_dspClockWhenPlaybackStarts + songStartOffset, 0);
            _songChannel.setPaused(false);
            
            _songChannel.setVolume(_songVolume);
            
            _channelGroup = SoundEffectManager.GetChannelGroup();

            system.getSoftwareFormat(
                out _frequency,
                out _,
                out _
            );

            isPlaying = true;
            
            SongClip.getLength(out uint songLength, TIMEUNIT.MS);
            _songLength = songLength / 1000f;
            
            SetCriticalSoundChannel(true);
        }

        private void SetCriticalSoundChannel(bool initialSet = false)
        {
            if (_criticalSoundIndex == NoteGenerator.Instance.criticalTimeList.Count)
                return;

            var sound = SoundEffectManager.CriticalSound;

            var system = SoundEffectManager.System;

            if (initialSet)
            {
                _channelGroup.getDSPClock(out _, out _dspClockWhenPlaybackStarts);
            }

            Channel channel;

            if (initialSet)
            {
                for (var i = 0; i < _criticalSoundChannelPool.Size; i++)
                {
                    _criticalSoundChannelPool.TryGetChannel(sound, out channel);

                    SetUpChannelDelay(channel);
                    channel.setVolume(_criticalSoundVolume);
                }

                return;
            }

            var channelAvailable = _criticalSoundChannelPool.TryGetChannel(sound, out channel);

            if (!channelAvailable)
                return;

            SetUpChannelDelay(channel);
            channel.setVolume(_criticalSoundVolume);

            return;

            void SetUpChannelDelay(Channel targetChannel)
            {
                var delay = (ulong)(_frequency * (_generalPlaybackDelayInSeconds +
                                                  NoteGenerator.Instance.criticalTimeList[_criticalSoundIndex] /
                                                  1000f));

                targetChannel.setDelay(_dspClockWhenPlaybackStarts + delay, 0);
                targetChannel.setPaused(false);

                _criticalSoundIndex++;
            }
        }


#if UNITY_EDITOR
        [InspectorButton("Skip Playback")]
        public void Skip()
        {
            _time = math.max(_songLength, NoteGenerator.Instance.endingTime / 1000f + 0.5f);
            _videoPlayer.Stop();
        }
#endif
    }
}

public class ChannelPool
{
    private readonly ChannelGroup _channelGroup;

    private readonly List<ChannelHandler> _pool = new();

    public readonly int Size;

    public ChannelPool(int size)
    {
        Size = size;

        _channelGroup = SoundEffectManager.GetChannelGroup();

        for (var i = 0; i < size; i++) _pool.Add(new ChannelHandler());
    }

    public bool TryGetChannel(Sound sound, out Channel channel)
    {
        foreach (var channelHandler in _pool)
        {
            if (!channelHandler.Channel.hasHandle())
            {
                channelHandler.Free = true;
                continue;
            }

            channelHandler.Channel.isPlaying(out var isPlaying);
            channelHandler.Channel.getPaused(out var paused);
            if (!isPlaying || paused)
            {
                channelHandler.Channel.stop();
                channelHandler.Channel.clearHandle();
                channelHandler.Channel = default;

                channelHandler.Free = true;
            }
        }

        for (var i = 0; i < Size; i++)
        {
            if (!_pool[i].Free)
                continue;

            SoundEffectManager.System.playSound(sound, _channelGroup, true, out _pool[i].Channel);

            _pool[i].Free = false;

            channel = _pool[i].Channel;
            return true;
        }

        channel = default;
        return false;
    }

    private class ChannelHandler
    {
        public Channel Channel;
        public bool Free = true;
    }
}