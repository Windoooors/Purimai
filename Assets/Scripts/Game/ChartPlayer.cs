using System.Linq;
using Game.Notes;
using LitMotion;
using UI;
using UI.Settings;
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
        private const float DefaultLandscapeSize = 5.5f;
        public static ChartPlayer Instance;

        public RawImage backgroundImage;

        public Image backgroundBrightnessCover;

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

        public bool isPlaying;

        public AudioClip songClip;

        [SerializeField] private float _dspTime;

        [SerializeField] private float _time;

        public int levelDifficultyIndex;

        private readonly float _generalPlaybackDelayInSeconds = 3f;

        private readonly int _maxScheduledCriticalSoundCount = 16;

        private double _audioDspTimeWhenPlaybackStarts;

        private Camera _camera;

        private bool _chartHasVideo;

        private int _criticalSoundIndex;

        private float _criticalSoundVolume;

        private bool _paused;
        private float _songLength;
        private AudioSourcePool.AudioSourceHandler _songPlaybackAudioSourceHandler;

        private float _songVolume;
        private VideoPlayer _videoPlayer;
        private RenderTexture _videoTexture;

        public Maidata Maidata;

        public void Awake()
        {
            Instance = this;

            _camera = FindAnyObjectByType<Camera>();

            ScreenOrientationManager.Instance.ScreenChanged += UpdateCameraSize;

            judgeDelay = SettingsPool.GetValue("input_delay");
            flowSpeed = SettingsPool.GetValue("flow_speed") * 0.25f + 1;
            slideAppearanceDelay = (SettingsPool.GetValue("slide_appearance_delay") - 10) / 10f;

            slideJudgeDisplayAnimationDuration = (int)(judgeDisplayAnimationClip.length * 1000);

            timeGapBeforeSlideStartsAppearing = (int)(2400 / flowSpeed * (1 - slideAppearanceDelay));
            slideFadeInDuration = timeGapBeforeSlideStartsAppearing > 200 ? timeGapBeforeSlideStartsAppearing : 200;

            SceneManager.sceneUnloaded += _ => { Destroy(_videoTexture); };

            _time = -_generalPlaybackDelayInSeconds;

            _criticalSoundVolume = SettingsPool.GetValue("volume.cue_sound") / 10f;
            _songVolume = SettingsPool.GetValue("volume.song") / 10f;

            UpdateCameraSize();
        }

        private void Update()
        {
            if (isPlaying)
                _dspTime = (float)AudioSettings.dspTime - (float)_audioDspTimeWhenPlaybackStarts -
                           _generalPlaybackDelayInSeconds;

            if (isPlaying && !_paused)
            {
                _time += Time.deltaTime;

                _time = (float)math.lerp(_time, _dspTime, 0.3);

                if (math.abs(_dspTime - _time) >= 0.05)
                    _time = _dspTime;

                SetCriticalSoundChannel();

                if (_time > math.max(NoteGenerator.GetInstance.endingTime / 1000f + 0.5f, _songLength))
                {
                    isPlaying = false;
                    LMotion.Create(_songVolume, 0, 0.5f).WithOnComplete(OnPlayCompleted)
                        .Bind(x => _songPlaybackAudioSourceHandler.SetVolume(x));
                }
            }
        }

        private void OnDestroy()
        {
            ScreenOrientationManager.Instance.ScreenChanged -= UpdateCameraSize;
        }

        private void ShowResult()
        {
            isPlaying = false;
            UIManager.GetInstance().ShowResult();
        }

        public void Pause(out bool succeed)
        {
            if (!isPlaying)
            {
                succeed = false;
                return;
            }

            AudioManager.GetInstance().AudioSourcePool.Pool.ForEach(x =>
            {
                if (x != _songPlaybackAudioSourceHandler)
                    x.Stop();
            });

            _criticalSoundIndex = NoteGenerator.GetInstance.criticalTimeList.FindLastIndex(x =>
                x < _time * 1000
            ) + 1;

            if (_criticalSoundIndex >= NoteGenerator.GetInstance.criticalTimeList.Count)
                _criticalSoundIndex = NoteGenerator.GetInstance.criticalTimeList.Count;

            _songPlaybackAudioSourceHandler.Pause();

            SimulatedSensor.Enabled = false;

            _videoPlayer?.Pause();

            _paused = true;

            succeed = true;
        }

        public void Resume()
        {
            if (_time > 0)
            {
                _audioDspTimeWhenPlaybackStarts = AudioSettings.dspTime -
                                                  _songPlaybackAudioSourceHandler.GetPosition() -
                                                  _generalPlaybackDelayInSeconds;

                _songPlaybackAudioSourceHandler.Play();
            }
            else
            {
                _audioDspTimeWhenPlaybackStarts = AudioSettings.dspTime -
                                                  _time -
                                                  _generalPlaybackDelayInSeconds;

                _songPlaybackAudioSourceHandler.Stop();

                AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out var handler);

                if (handler == null)
                    return;

                _songPlaybackAudioSourceHandler = handler;

                _songPlaybackAudioSourceHandler.SetClip(Maidata.SongAudioClip);
                _songPlaybackAudioSourceHandler.SetVolume(_songVolume);

                _songPlaybackAudioSourceHandler.PlayScheduled(AudioSettings.dspTime - _time);
            }

            SetCriticalSoundChannel(true);

            _videoPlayer?.Play();

            SimulatedSensor.Enabled = true;

            _paused = false;
        }

        private bool TryLoadVideo(string path)
        {
            if (path == "" || SettingsPool.GetValue("background_video_playback") == 0)
                return false;

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

            return true;
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

        private void InitializeCircleColor(int index, bool isUtage)
        {
            var targetIndex = isUtage ? 5 : index == 5 ? 4 : index;

            if (judgeCircleColors.Length <= targetIndex || targetIndex < 0)
                targetIndex = 5;

            judgeCircleSpriteRenderer.color = judgeCircleColors[targetIndex];
            judgeCircleGlowSpriteRenderer.color = judgeCircleColors[targetIndex];
        }

        private void OnPlayCompleted()
        {
            _songPlaybackAudioSourceHandler.Stop();

            ShowResult();
        }

        private void Play()
        {
            LMotion.Create(_generalPlaybackDelayInSeconds * 1000f, 0, _generalPlaybackDelayInSeconds)
                .WithEase(Ease.Linear).WithOnComplete(() =>
                {
                    if (_chartHasVideo)
                    {
                        _videoPlayer.Play();
                        backgroundImage.color = Color.white;
                    }
                }).Bind(_ => { });

            isPlaying = true;

            PlaySound();
        }

        public void UpdateCameraSize()
        {
            var aspectRatio = (float)Screen.currentResolution.width / Screen.currentResolution.height;

            if (aspectRatio >= 1.0f)
                _camera.orthographicSize = DefaultLandscapeSize;
            else
                _camera.orthographicSize = DefaultLandscapeSize / aspectRatio;
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
            if (_criticalSoundIndex == NoteGenerator.GetInstance.criticalTimeList.Count)
                return;

            AudioSourcePool.AudioSourceHandler handler;

            if (initialSet)
            {
                for (var i = 0; i < _maxScheduledCriticalSoundCount; i++)
                {
                    AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out handler);

                    SetUpChannelDelay(handler);
                    handler?.SetVolume(_criticalSoundVolume);
                }

                return;
            }

            if (AudioManager.GetInstance().AudioSourcePool.GetOccupiedCount() < _maxScheduledCriticalSoundCount)
            {
                var channelAvailable = AudioManager.GetInstance().AudioSourcePool.TryGetAudioSourceHandler(out handler);

                if (!channelAvailable)
                    return;

                SetUpChannelDelay(handler);
                handler?.SetVolume(_criticalSoundVolume);
            }

            return;

            void SetUpChannelDelay(AudioSourcePool.AudioSourceHandler audioSourceHandler)
            {
                if (_criticalSoundIndex >= NoteGenerator.GetInstance.criticalTimeList.Count ||
                    audioSourceHandler == null)
                    return;

                var delay = _generalPlaybackDelayInSeconds +
                            NoteGenerator.GetInstance.criticalTimeList[_criticalSoundIndex] /
                            1000f + _audioDspTimeWhenPlaybackStarts;

                var audioClip = AudioManager.GetInstance().criticalSound;

                audioSourceHandler.SetClip(audioClip);
                audioSourceHandler.PlayScheduled(delay);
                audioSourceHandler.ScheduledStartTime = delay;

                _criticalSoundIndex++;
            }
        }

        public void InitializeLevel(Maidata maidata, int difficultyIndex)
        {
            Maidata = maidata;

            levelDifficultyIndex = difficultyIndex;

            var chart = maidata.Charts.ToList().Find(x => x.DifficultyIndex == difficultyIndex);

            NoteGenerator.GetInstance.GenerateNotes(chart.ChartString, maidata.FirstNoteTime);

            songClip = maidata.SongAudioClip;

            var useBlurredCover = SettingsPool.GetValue("blurred_cover") != 0;

            if (!TryLoadVideo(maidata.PvPath))
            {
                backgroundImage.color = Color.white;
                backgroundImage.texture = useBlurredCover
                    ? maidata.BlurredSongCoverAsBackgroundDecodedImage.GetTexture2D()
                    : maidata.SongCoverDecodedImage.GetTexture2D();
            }
            else
            {
                backgroundImage.texture = null;
                backgroundImage.color = Color.black;
            }

            InitializeCircleColor(difficultyIndex - 1, maidata.IsUtage);

            var darkness = SettingsPool.GetValue("background_brightness") switch
            {
                0 => 1f,
                1 => 0.7f,
                2 => 0.435f,
                3 => 0.3f,
                4 => 0.2f,
                5 => 0,
                _ => 0.435f
            };

            backgroundBrightnessCover.color = new Color(0, 0, 0, darkness);

            Play();
        }


#if UNITY_EDITOR
        [InspectorButton("Skip Playback")]
        public void Skip()
        {
            _time = math.max(_songLength, NoteGenerator.GetInstance.endingTime / 1000f + 0.5f);
            _songPlaybackAudioSourceHandler.Pause();
            _videoPlayer?.Stop();
            ShowResult();
        }
#endif
    }
}