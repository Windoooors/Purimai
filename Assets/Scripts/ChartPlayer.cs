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
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && !isPlaying)
            Play();

        isPlaying = audioSource.isPlaying;

        if (isPlaying)
            time = (int)(audioSource.time * 1000);
    }

    public void Play()
    {
        audioSource.Play();
    }
}