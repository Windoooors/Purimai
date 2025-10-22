using Notes;
using UnityEngine;

public class ChartPlayer : MonoBehaviour
{
    public static ChartPlayer Instance;
    public AudioSource audioSource;

    public int time;

    public bool isPlaying;

    public float flowSpeed;
    public int starAppearanceDelay;
    public int starAppearanceDuration;
    public int slideJudgeDisplayAnimationDuration = 600;
    
    public Animator[] holdRippleAnimators;
    public Animator[] aAreaRippleAnimators;

    public AnimationClip judgeDisplayAnimationClip;

    public JudgeSettings tapJudgeSettings;
    public JudgeSettings slideJudgeSettings;
    public JudgeSettings holdTailJudgeSettings;

    public void Awake()
    {
        Instance = this;

        slideJudgeDisplayAnimationDuration = (int)(judgeDisplayAnimationClip.length * 1000);

        SimulatedSensor.OnTap += (sender, args) =>
        {
            if (!isPlaying)
                Play();
        };
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