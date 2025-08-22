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

    public void Awake()
    {
        Instance = this;

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