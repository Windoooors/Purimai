using System.Collections;
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

        public int levelDifficultyIndex;

        private readonly float _generalPlaybackDelayInSeconds = 3f;

        private readonly float _maxCalibrationRate = 0.1f;

        private readonly int _maxScheduledCriticalSoundCount = 16;
        private int _calibrationTimes;

        private Camera _camera;

        private bool _chartHasVideo;

        private int _criticalSoundIndex;

        private float _criticalSoundVolume;

        private double _dspTimeWhenPausing;

        private double _dspTimeWhenSongStartsPlaying;

        private float _needCalibrationThreshold = 1f;

        private bool _paused;

        private float _songLength;
        private AudioSourcePool.AudioSourceHandler _songPlaybackAudioSourceHandler;

        private float _songVolume;

        private bool _startCalibrated;

        private float _time;
        private VideoPlayer _videoPlayer;
        private RenderTexture _videoTexture;

        public Maidata Maidata;

        public float TimeInMilliseconds => _time * 1000f;

        public void Awake()
        {
            Instance = this;

            _camera = FindAnyObjectByType<Camera>();

            _needCalibrationThreshold = PlayerPrefs.GetFloat("CalibrationThreshold", 1f);
            
            ScreenOrientationManager.Instance.ScreenChanged = null;
            ScreenOrientationManager.Instance.ScreenChanged += UpdateCameraSize;

            judgeDelay = SettingsPool.GetValue("input_delay");
            flowSpeed = SettingsPool.GetValue("flow_speed") * 0.25f + 1;

            if (flowSpeed.Equals(10.25f)) flowSpeed = 49;

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
            if (!isPlaying || _paused)
                return;

            _time += Time.deltaTime;

            CalibrateTime();

            SetCueSoundChannel();

            if (_time > math.max(NoteGenerator.Instance.endingTime / 1000f + 0.5f, _songLength))
            {
                isPlaying = false;
                LMotion.Create(_songVolume, 0, 0.5f).WithOnComplete(OnPlayCompleted)
                    .Bind(x => _songPlaybackAudioSourceHandler.SetVolume(x));
            }

            NoteGenerator.Instance.notesList.ForEach(noteBase =>
            {
                if (_time + 0.1f >= noteBase.emergingTime / 1000f && noteBase.enabled)
                    noteBase.ManualUpdate();
            });
        }

        private float _songPositionWhenCalibrationThresholdChanged;
        
        private void CalibrateTime()
        {
            var songPosition = _songPlaybackAudioSourceHandler.GetPosition();

            if (!_startCalibrated &&
                songPosition > 0)
            {
                _startCalibrated = true;
                _time = songPosition;
            }

            if (songPosition > 0 && math.abs(_time - songPosition) >
                _needCalibrationThreshold * (Time.deltaTime + AudioManager.Instance.DSPDurationInSeconds))
            {
                _calibrationTimes++;
                _time = songPosition;

                if (_calibrationTimes / (songPosition - _songPositionWhenCalibrationThresholdChanged) > _maxCalibrationRate)
                {
                    _calibrationTimes = 0;
                    _songPositionWhenCalibrationThresholdChanged = songPosition;
                    _needCalibrationThreshold += 0.05f;
                    
                    PlayerPrefs.SetFloat("CalibrationThreshold", _needCalibrationThreshold);
                }
            }

            /*if (songPosition > 0 && math.abs(_time - songPosition) >
                _needLerpCalibrationThreshold * (Time.deltaTime + AudioManager.Instance.DSPDurationInSeconds))
            {
                _time = math.lerp(_time, songPosition, 0.1f);
            }*/
        }

        private void ShowResult()
        {
            isPlaying = false;
            UIManager.Instance.ShowResult();
        }

        public void Pause(out bool succeed)
        {
            if (!isPlaying || _paused)
            {
                succeed = false;
                return;
            }

            AudioManager.Instance.AudioSourcePool.Pool.ForEach(x =>
            {
                if (x != _songPlaybackAudioSourceHandler)
                    x.Stop();
            });

            _criticalSoundIndex = NoteGenerator.Instance.CriticalTimeList.FindLastIndex(x =>
                x < _time * 1000
            ) + 1;

            if (_criticalSoundIndex >= NoteGenerator.Instance.CriticalTimeList.Count)
                _criticalSoundIndex = NoteGenerator.Instance.CriticalTimeList.Count;

            _songPlaybackAudioSourceHandler.Pause();

            SimulatedSensor.Enabled = false;

            _videoPlayer?.Pause();

            _paused = true;

            succeed = true;
        }

        public void Resume()
        {
            StartCoroutine(ResumeRoutine());

            return;

            IEnumerator ResumeRoutine()
            {
                double scheduledTime;

                if (_time > 0)
                {
                    scheduledTime = AudioManager.Instance.EstimatedDspTime + 0.5;
                    _dspTimeWhenSongStartsPlaying = scheduledTime - _time;
                }
                else
                {
                    scheduledTime = AudioManager.Instance.EstimatedDspTime - _time + 0.5;
                    _dspTimeWhenSongStartsPlaying = scheduledTime;
                }

                _songPlaybackAudioSourceHandler.Stop();

                AudioManager.Instance.AudioSourcePool.TryGetAudioSourceHandler(out var handler);

                if (handler == null)
                    yield break;

                _songPlaybackAudioSourceHandler = handler;

                _songPlaybackAudioSourceHandler.SetClip(Maidata.SongAudioClip);
                _songPlaybackAudioSourceHandler.SetVolume(_songVolume);
                _songPlaybackAudioSourceHandler.SetPosition(_time);

                _songPlaybackAudioSourceHandler.PlayScheduled(scheduledTime);

                SetCueSoundChannel(true);

                yield return new WaitForSeconds(0.5f);

                _videoPlayer?.Play();

                SimulatedSensor.Enabled = true;

                _paused = false;
            }
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
            StartCoroutine(VideoPlaybackRoutine());

            isPlaying = true;

            PlayAudio();

            return;

            IEnumerator VideoPlaybackRoutine()
            {
                yield return new WaitForSeconds(_generalPlaybackDelayInSeconds);

                if (_chartHasVideo)
                {
                    _videoPlayer.Play();
                    backgroundImage.color = Color.white;
                }
            }
        }

        private void UpdateCameraSize()
        {
            var aspectRatio = (float)Screen.currentResolution.width / Screen.currentResolution.height;

            if (aspectRatio >= 1.0f)
                _camera.orthographicSize = DefaultLandscapeSize;
            else
                _camera.orthographicSize = DefaultLandscapeSize / aspectRatio;
        }

        private void PlayAudio()
        {
            AudioManager.Instance.AudioSourcePool.TryGetAudioSourceHandler(out _songPlaybackAudioSourceHandler);

            _songPlaybackAudioSourceHandler.SetClip(songClip);

            _dspTimeWhenSongStartsPlaying = AudioManager.Instance.EstimatedDspTime +
                                            _generalPlaybackDelayInSeconds;

            _songPlaybackAudioSourceHandler.PlayScheduled(_dspTimeWhenSongStartsPlaying);

            _songPlaybackAudioSourceHandler.ScheduledStartTime = _dspTimeWhenSongStartsPlaying;

            _songPlaybackAudioSourceHandler.SetVolume(_songVolume);

            isPlaying = true;

            _songLength = songClip.length;

            SetCueSoundChannel(true);
        }

        private void SetCueSoundChannel(bool initialSet = false)
        {
            if (_criticalSoundIndex == NoteGenerator.Instance.CriticalTimeList.Count)
                return;

            AudioSourcePool.AudioSourceHandler handler;

            if (initialSet)
            {
                for (var i = 0; i < _maxScheduledCriticalSoundCount; i++)
                {
                    AudioManager.Instance.AudioSourcePool.TryGetAudioSourceHandler(out handler);

                    SetUpChannelDelay(handler);
                    handler?.SetVolume(_criticalSoundVolume);
                }

                return;
            }

            if (AudioManager.Instance.AudioSourcePool.GetOccupiedCount() < _maxScheduledCriticalSoundCount)
            {
                var channelAvailable = AudioManager.Instance.AudioSourcePool.TryGetAudioSourceHandler(out handler);

                if (!channelAvailable)
                    return;

                SetUpChannelDelay(handler);
                handler?.SetVolume(_criticalSoundVolume);
            }

            return;

            void SetUpChannelDelay(AudioSourcePool.AudioSourceHandler audioSourceHandler)
            {
                if (_criticalSoundIndex >= NoteGenerator.Instance.CriticalTimeList.Count ||
                    audioSourceHandler == null)
                    return;

                var delay = _dspTimeWhenSongStartsPlaying +
                            NoteGenerator.Instance.CriticalTimeList[_criticalSoundIndex] /
                            1000f;

                var audioClip = AudioManager.Instance.criticalSound;

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

            NoteGenerator.Instance.GenerateNotes(chart.ChartString, maidata.FirstNoteTime);

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
            _time = math.max(_songLength, NoteGenerator.Instance.endingTime / 1000f + 0.5f);
            _songPlaybackAudioSourceHandler.Pause();
            _videoPlayer?.Stop();
            ShowResult();
        }
#endif
    }
}