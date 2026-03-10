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

        private const int GlobalInputOffset = 11;
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

        public int levelDifficultyIndex;

        private readonly float _generalPlaybackDelayInSeconds = 3f;
        private int _calibrationTimes;

        private Camera _camera;

        private bool _chartHasVideo;

        private float _criticalSoundVolume;

        private int _cueSoundIndex;

        private Coroutine _delayedAudioPlaybackRoutine;

        private Coroutine _delayedVideoPlaybackRoutine;
        private readonly float _maxCalibrationRate = 0.1f;
        private float _needCalibrationThreshold = 0.020f;

        private bool _paused;

        private float _songLengthInSeconds;
        private float _songPositionWhenCalibrationThresholdChanged;

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

            _needCalibrationThreshold = PlayerPrefs.GetFloat("CalibrationDeltaTimeThreshold", 0.020f);

            _camera = FindAnyObjectByType<Camera>();

            ScreenOrientationManager.Instance.ScreenChanged += UpdateCameraSize;

            judgeDelay = SettingsPool.GetValue("input_delay") + GlobalInputOffset;
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

            if (_time > math.max(NoteGenerator.Instance.endingTime / 1000f + 0.5f, _songLengthInSeconds))
            {
                isPlaying = false;
                LMotion.Create(_songVolume, 0, 0.5f).WithOnComplete(OnPlayCompleted)
                    .Bind(x => Maidata.SongBassHandler.Volume = x);
            }

            NoteGenerator.Instance.notesList.ForEach(noteBase =>
            {
                if (_time + 0.1f >= noteBase.emergingTime / 1000f && noteBase.enabled)
                    noteBase.ManualUpdate();
            });
        }

        private void LateUpdate()
        {
            if (NoteGenerator.Instance.CriticalTimeList.Count == 0)
                return;

            if (_cueSoundIndex >= NoteGenerator.Instance.CriticalTimeList.Count)
                return;

            var audioTime = Maidata.SongBassHandler?.GetPosition();

            if (_time <= 0 || !(Maidata.SongBassHandler?.IsPlaying ?? false))
                audioTime = _time;

            if (audioTime * 1000 >= NoteGenerator.Instance.CriticalTimeList[_cueSoundIndex])
            {
                SfxManager.Instance.PlayCueSound();
                _cueSoundIndex++;
            }
        }

        private void OnDestroy()
        {
            ScreenOrientationManager.Instance.ScreenChanged -= UpdateCameraSize;
        }

        private void CalibrateTime()
        {
            if (!Maidata.SongBassHandler.IsPlaying)
                return;
            
            var songPosition = (float)Maidata.SongBassHandler.GetPosition();

            if (!_startCalibrated &&
                songPosition > 0)
            {
                _startCalibrated = true;
                _time = songPosition;
            }

            if (songPosition > 0 && math.abs(_time - songPosition) >
                _needCalibrationThreshold)
            {
                _calibrationTimes++;
                _time = songPosition;

                if (_calibrationTimes / (songPosition - _songPositionWhenCalibrationThresholdChanged) >
                    _maxCalibrationRate)
                {
                    _calibrationTimes = 0;
                    _songPositionWhenCalibrationThresholdChanged = songPosition;
                    _needCalibrationThreshold += 0.002f;

                    PlayerPrefs.SetFloat("CalibrationDeltaTimeThreshold", _needCalibrationThreshold);
                }
            }
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

            SimulatedSensor.Enabled = false;

            if (_time < 0)
            {
                if (_delayedVideoPlaybackRoutine != null)
                {
                    StopCoroutine(_delayedVideoPlaybackRoutine);
                    _delayedVideoPlaybackRoutine = null;
                }

                if (_delayedAudioPlaybackRoutine != null)
                {
                    StopCoroutine(_delayedAudioPlaybackRoutine);
                    _delayedAudioPlaybackRoutine = null;
                }
            }

            _videoPlayer?.Pause();
            Maidata.SongBassHandler.Pause();

            _paused = true;

            succeed = true;
        }

        public void Resume()
        {
            StartCoroutine(ResumeRoutine());

            return;

            IEnumerator ResumeRoutine()
            {
                float scheduledDelay;

                if (_time > 0)
                    scheduledDelay = 0.5f;
                else
                    scheduledDelay = _time + 0.5f;

                _delayedVideoPlaybackRoutine = StartCoroutine(VideoPlaybackRoutine(scheduledDelay));
                _delayedAudioPlaybackRoutine = StartCoroutine(AudioPlaybackRoutine(scheduledDelay));

                yield return new WaitForSeconds(scheduledDelay);
                if (SettingsPool.GetValue("auto_play") != 1)
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
            Maidata.SongBassHandler.Stop();

            ShowResult();
        }

        private void Play()
        {
            _delayedVideoPlaybackRoutine = StartCoroutine(VideoPlaybackRoutine(_generalPlaybackDelayInSeconds));

            isPlaying = true;

            PlayAudio();
        }


        private IEnumerator AudioPlaybackRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            Maidata.SongBassHandler.Play();
        }

        private IEnumerator VideoPlaybackRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_chartHasVideo)
            {
                _videoPlayer.Play();
                backgroundImage.color = Color.white;
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
            isPlaying = true;

            _songLengthInSeconds = (float)Maidata.SongBassHandler.Duration;

            _delayedAudioPlaybackRoutine = StartCoroutine(AudioPlaybackRoutine(_generalPlaybackDelayInSeconds));
        }

        public void InitializeLevel(Maidata maidata, int difficultyIndex)
        {
            Maidata = maidata;

            levelDifficultyIndex = difficultyIndex;

            var chart = maidata.Charts.ToList().Find(x => x.DifficultyIndex == difficultyIndex);

            NoteGenerator.Instance.GenerateNotes(chart.ChartString, maidata.FirstNoteTime);

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
            _time = math.max(_songLengthInSeconds, NoteGenerator.Instance.endingTime / 1000f + 0.5f);
            Maidata.SongBassHandler.Pause();
            _videoPlayer?.Stop();
            ShowResult();
        }
#endif
    }
}