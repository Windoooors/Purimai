using Game.Notes;
using LitMotion;
using UI;
using UI.GameSettings;
using UI.Result;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        public int timeGapBeforeSlideStartsAppearing;
        [HideInInspector] public int slideFadeInDuration;

        public Animator[] holdRippleAnimators;
        public Animator judgeCircleGlowAnimator;

        public AnimationClip judgeDisplayAnimationClip;

        public JudgeSettings tapJudgeSettings;
        public JudgeSettings slideJudgeSettings;
        public JudgeSettings holdTailJudgeSettings;

        [SerializeField] private float _time;

        public bool isPlaying;

        public AudioClip songClip;

        [SerializeField] private float _dspTime;

        private readonly float _generalPlaybackDelayInSeconds = 3f;

        private readonly int _maxScheduledCriticalSoundCount = 16;

        private double _audioDspTimeWhenPlaybackStarts;

        private bool _chartHasVideo;

        private int _criticalSoundIndex;

        private float _criticalSoundVolume;

        private bool _paused;
        private float _songLength;
        private AudioSourcePool.AudioSourceHandler _songPlaybackAudioSourceHandler;

        private float _songVolume;
        private VideoPlayer _videoPlayer;
        private RenderTexture _videoTexture;

        public void Awake()
        {
            Instance = this;

            judgeDelay = SettingsPool.GetValue("game.judge_delay");
            flowSpeed = SettingsPool.GetValue("game.flow_speed") * 0.25f + 1;
            slideAppearanceDelay = (SettingsPool.GetValue("game.slide_delay") - 10) / 10f;

            slideJudgeDisplayAnimationDuration = (int)(judgeDisplayAnimationClip.length * 1000);

            timeGapBeforeSlideStartsAppearing = (int)(2400 / flowSpeed * (1 - slideAppearanceDelay));
            slideFadeInDuration = timeGapBeforeSlideStartsAppearing > 200 ? timeGapBeforeSlideStartsAppearing : 200;

            SceneManager.sceneUnloaded += _ => { Destroy(_videoTexture); };

            _time = -_generalPlaybackDelayInSeconds;

            _criticalSoundVolume = SettingsPool.GetValue("game.volume.critical_sound") / 10f;
            _songVolume = SettingsPool.GetValue("game.volume.song") / 10f;
        }

        private void Update()
        {
            if (isPlaying)
                _dspTime = (float)AudioSettings.dspTime - (float)_audioDspTimeWhenPlaybackStarts -
                           _generalPlaybackDelayInSeconds;

            if (isPlaying && !_paused)
            {
                _time += Time.deltaTime;
                
                SetCriticalSoundChannel();

                if (_time > math.max(NoteGenerator.Instance.endingTime / 1000f + 0.5f, _songLength))
                {
                    isPlaying = false;
                    LMotion.Create(_songVolume, 0, 0.5f).WithOnComplete(OnPlayCompleted)
                        .Bind(x => _songPlaybackAudioSourceHandler.SetVolume(x));
                }
            }
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
            _songPlaybackAudioSourceHandler.Pause();
            _paused = true;
        }

        private void Resume()
        {
            _paused = false;
        }

        public void LoadVideo(string path)
        {
            if (path == "" || SettingsPool.GetValue("game.background_video_playback") == 0)
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

        public float GetTime(bool getDspTime = false)
        {
            if (_songPlaybackAudioSourceHandler != null)
                return (getDspTime
                    ? _songPlaybackAudioSourceHandler.IsPlaying()
                        ? (float)AudioSettings.dspTime - (float)_audioDspTimeWhenPlaybackStarts -
                          _generalPlaybackDelayInSeconds
                        : _time
                    : _time) * 1000;

            return _time;
        }

        public void InitializeCircleColor(int index, bool isUtage)
        {
            judgeCircleSpriteRenderer.color = judgeCircleColors[isUtage ? 5 : index == 5 ? 4 : index];
            judgeCircleGlowSpriteRenderer.color = judgeCircleColors[isUtage ? 5 : index == 5 ? 4 : index];
        }

        private void OnPlayCompleted()
        {
            _songPlaybackAudioSourceHandler.Stop();

            UIManager.GetInstance().EnableUI();
            ResultController.GetInstance().ShowResult();
        }

        public void Play()
        {
            LMotion.Create(_generalPlaybackDelayInSeconds * 1000f, 0, _generalPlaybackDelayInSeconds)
                .WithEase(Ease.Linear).WithOnComplete(() =>
                {
                    if (_chartHasVideo) _videoPlayer.Play();
                }).Bind(_ => { });

            isPlaying = true;

            PlaySound();
        }

        private void PlaySound()
        {
            _audioDspTimeWhenPlaybackStarts = AudioSettings.dspTime;

            AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out _songPlaybackAudioSourceHandler);

            _songPlaybackAudioSourceHandler.SetClip(songClip);

            var delay = _audioDspTimeWhenPlaybackStarts +
                        _generalPlaybackDelayInSeconds;

            _songPlaybackAudioSourceHandler.PlayScheduled(delay);

            _songPlaybackAudioSourceHandler.ScheduledStartTime = delay;

            _songPlaybackAudioSourceHandler.SetVolume(_songVolume);

            isPlaying = true;

            _songLength = songClip.length;

            SetCriticalSoundChannel(true);
        }

        private void SetCriticalSoundChannel(bool initialSet = false)
        {
            if (_criticalSoundIndex == NoteGenerator.Instance.criticalTimeList.Count)
                return;

            AudioSourcePool.AudioSourceHandler handler;

            if (initialSet)
            {
                for (var i = 0; i < _maxScheduledCriticalSoundCount; i++)
                {
                    AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out handler);

                    SetUpChannelDelay(handler);
                    handler.SetVolume(_criticalSoundVolume);
                }

                return;
            }

            if (AudioManager.GetInstance().AudioSourcePool.GetOccupiedCount() < _maxScheduledCriticalSoundCount)
            {
                var channelAvailable = AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out handler);

                if (!channelAvailable)
                    return;

                SetUpChannelDelay(handler);
                handler.SetVolume(_criticalSoundVolume);
            }

            return;

            void SetUpChannelDelay(AudioSourcePool.AudioSourceHandler handler)
            {
                if (_criticalSoundIndex >= NoteGenerator.Instance.criticalTimeList.Count)
                    return;

                var delay = _generalPlaybackDelayInSeconds +
                            NoteGenerator.Instance.criticalTimeList[_criticalSoundIndex] /
                            1000f + _audioDspTimeWhenPlaybackStarts;

                var audioClip = AudioManager.GetInstance().criticalSound;

                handler.SetClip(audioClip);
                handler.PlayScheduled(delay);
                handler.ScheduledStartTime = delay;

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