using Game.Notes;
using UnityEngine;

namespace Game
{
    public class ChartPlayer : MonoBehaviour
    {
        public static ChartPlayer Instance;
        public AudioSource audioSource;

        public int time;

        public bool isPlaying;

        public float flowSpeed;
        public float slideAppearanceDelay;

        public int slideJudgeDisplayAnimationDuration = 600;
        public int slideConcealDelay = 33;

        [HideInInspector] public int slideAppearanceDeltaTime;
        [HideInInspector] public int slideFadeInDuration;

        public Animator[] holdRippleAnimators;
        public Animator judgeCircleGlowAnimator;

        public AnimationClip judgeDisplayAnimationClip;

        public JudgeSettings tapJudgeSettings;
        public JudgeSettings slideJudgeSettings;
        public JudgeSettings holdTailJudgeSettings;

        public void Awake()
        {
            Application.targetFrameRate = 120;

            Instance = this;

            slideJudgeDisplayAnimationDuration = (int)(judgeDisplayAnimationClip.length * 1000);

            SimulatedSensor.OnTap += (_, _) =>
            {
                if (!isPlaying)
                    Play();
            };

            slideAppearanceDeltaTime = -(int)(2400 / flowSpeed * (1 - slideAppearanceDelay));
            slideFadeInDuration = -slideAppearanceDeltaTime > 200 ? 200 : -slideAppearanceDeltaTime;
        }

        private void Update()
        {
            isPlaying = audioSource.isPlaying;

            if (isPlaying)
                time = (int)(audioSource.time * 1000);
        }

        public void Play()
        {
            audioSource.Play();
        }
    }
}