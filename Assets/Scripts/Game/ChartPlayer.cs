using System.Collections.Generic;
using FMOD;
using FMODUnity;
using Game.Notes;
using LitMotion;
using UI;
using UI.GameSettings;
using UI.Result;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using EditorScript;
#endif

namespace Game
{
    public class ChartPlayer : MonoBehaviour
    {
        public static ChartPlayer Instance;

        public Image backgroundImage;

        public int time;
        public int lastFrameTime;

        public bool isPlaying;

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

        private readonly float _generalPlaybackDelayInSeconds = 3f;

        private bool _audioPlaybackStarted;

        private Channel _channel;
        private ChannelGroup _channelGroup;

        private ChannelPool _criticalSoundChannelPool;

        private int _criticalSoundIndex;

        private ulong _dspClock;
        private int _frequency;
        private bool _isPlayingOnLastFrame;

        public Sound SongClip;

        public void Awake()
        {
            Instance = this;

            judgeDelay = SettingsPool.GetValue("game.judge_delay");
            flowSpeed = SettingsPool.GetValue("game.flow_speed") * 0.25f + 1;
            slideAppearanceDelay = (SettingsPool.GetValue("game.slide_delay") - 10) / 10f;

            slideJudgeDisplayAnimationDuration = (int)(judgeDisplayAnimationClip.length * 1000);

            slideAppearanceDeltaTime = -(int)(2400 / flowSpeed * (1 - slideAppearanceDelay));
            slideFadeInDuration = -slideAppearanceDeltaTime > 200 ? 200 : -slideAppearanceDeltaTime;

            time = -(int)(_generalPlaybackDelayInSeconds * 1000);

            _criticalSoundChannelPool = new ChannelPool(23, SoundEffectManager.CriticalSound);
        }

        private void Update()
        {
            if (!_audioPlaybackStarted)
                return;

            _channel.isPlaying(out isPlaying);

            if (!isPlaying && _isPlayingOnLastFrame)
            {
                OnPlayCompleted();
                _isPlayingOnLastFrame = false;
            }

            if (isPlaying)
            {
                lastFrameTime = time;
                time = GetTime();
                _isPlayingOnLastFrame = true;

                SetChannel();
            }
        }

        public int GetTime()
        {
            if (!_channel.hasHandle())
                return time;

            _channelGroup.getDSPClock(out _, out var currentDSPClock);
            return (int)((currentDSPClock - (_dspClock + _frequency * _generalPlaybackDelayInSeconds)) / _frequency *
                         1000);
        }

        public void InitializeCircleColor(int index, bool isUtage)
        {
            judgeCircleSpriteRenderer.color = judgeCircleColors[isUtage ? 5 : index == 5 ? 4 : index];
            judgeCircleGlowSpriteRenderer.color = judgeCircleColors[isUtage ? 5 : index == 5 ? 4 : index];
        }

        private void OnPlayCompleted()
        {
            UIManager.GetInstance().EnableUI();
            ResultController.GetInstance().ShowResult();
        }

        public void Play()
        {
            LMotion.Create(-(int)(_generalPlaybackDelayInSeconds * 1000), 0, _generalPlaybackDelayInSeconds)
                .WithEase(Ease.Linear).WithOnComplete(() => _audioPlaybackStarted = true).Bind(x => time = x);

            PlaySound();

            isPlaying = true;
        }

        private void PlaySound()
        {
            SetChannel(true);
            
            var system = RuntimeManager.CoreSystem;

            system.getMasterChannelGroup(out var masterGroup);

            system.getSoftwareFormat(
                out var frequency,
                out _,
                out _
            );

            masterGroup.getDSPClock(out _, out var dspClock);

            var songStartOffset = (ulong)(frequency * _generalPlaybackDelayInSeconds);

            system.playSound(SongClip, masterGroup, true, out _channel);
            _channel.setDelay(dspClock + songStartOffset, 0);
            _channel.setPaused(false);
        }

        private void SetChannel(bool initialSet = false)
        {
            if (_criticalSoundIndex == NoteGenerator.Instance.criticalTimeList.Count)
                return;

            var system = RuntimeManager.CoreSystem;

            if (initialSet)
            {
                system.getMasterChannelGroup(out _channelGroup);

                system.getSoftwareFormat(
                    out _frequency,
                    out _,
                    out _
                );
                _channelGroup.getDSPClock(out _, out _dspClock);
            }

            Channel channel;

            if (initialSet)
            {
                for (var i = 0; i < _criticalSoundChannelPool.Size; i++)
                {
                    _criticalSoundChannelPool.TryGetChannel(out channel);

                    SetUpChannelDelay(channel);
                }

                return;
            }

            var channelAvailable = _criticalSoundChannelPool.TryGetChannel(out channel);

            if (!channelAvailable)
                return;

            SetUpChannelDelay(channel);

            return;

            void SetUpChannelDelay(Channel targetChannel)
            {
                var delay = (ulong)(_frequency * (_generalPlaybackDelayInSeconds +
                                                  NoteGenerator.Instance.criticalTimeList[_criticalSoundIndex] /
                                                  1000f));

                targetChannel.setDelay(_dspClock + delay, 0);
                targetChannel.setPaused(false);

                _criticalSoundIndex++;
            }
        }


#if UNITY_EDITOR
        [InspectorButton("Skip Playback")]
        public void Skip()
        {
            _channel.stop();
        }
#endif
    }
}

public class ChannelPool
{
    private readonly List<ChannelHandler> _pool = new();

    public readonly int Size;

    private readonly Sound _criticalSound;
    
    private readonly ChannelGroup _channelGroup;

    public ChannelPool(int size, Sound criticalSound)
    {
        Size = size;
        
        _criticalSound = criticalSound;
        
        RuntimeManager.CoreSystem.getMasterChannelGroup(out _channelGroup);

        for (var i = 0; i < size; i++) _pool.Add(new ChannelHandler());
    }

    public bool TryGetChannel(out Channel channel)
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
            
            RuntimeManager.CoreSystem.playSound(_criticalSound, _channelGroup, true, out _pool[i].Channel);

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