using FMOD;
using FMODUnity;
using Game.Notes;
using UI;
using UI.GameSettings;
using UI.Result;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ChartPlayer : MonoBehaviour
    {
        public static ChartPlayer Instance;

        public Image backgroundImage;

        public int time;

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

        private Channel _channel;
        private bool _isPlayingOnLastFrame;

        private bool _playbackStarted;

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
        }

        private void Update()
        {
            if (!_playbackStarted)
                return;

            _channel.isPlaying(out isPlaying);

            if (!isPlaying && _isPlayingOnLastFrame)
            {
                OnPlayCompleted();
                _isPlayingOnLastFrame = false;
            }

            if (isPlaying)
            {
                _channel.getPosition(out var unsignedTime, TIMEUNIT.MS);
                time = (int)unsignedTime;
                _isPlayingOnLastFrame = true;
            }
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
            RuntimeManager.CoreSystem.playSound(SongClip, default, false, out _channel);
            _playbackStarted = true;
        }
    }
}