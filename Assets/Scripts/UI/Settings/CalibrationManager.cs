using System.Collections;
using System.Linq;
using Game;
using LitMotion;
using UI.LevelSelection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class CalibrationManager : MonoBehaviour
    {
        private static CalibrationManager _instance;

        public VisualTreeAsset calibrationTreeAsset;

        private readonly float[] _beats =
        {
            1f, 3, 5f, 7
        };

        private readonly float[] _deltaTimeArray = { 0, 0, 0, 0 };

        private readonly Color _originalEffectColor = new(217 / 255f, 217 / 255f, 217 / 255f);

        private VisualElement _background;

        private BassHandler _bassHandler;

        private int _beatIndex;

        private bool _calibrationStarted;
        private VisualElement _calibrationTree;
        private VisualElement _cover;

        private MotionHandle _effectHandle;
        private Label _hintLabel;
        private Label _hintTappedLabel;
        private float _inputOffset;

        private StyleSheet _preAnimatedSheet;
        private Button _returnButton;

        private MotionHandle _songFadeOutHandle;

        private float _timerInSeconds;
        public static CalibrationManager Instance => _instance ??= FindAnyObjectByType<CalibrationManager>();

        private void Awake()
        {
            _instance = this;

            _inputOffset = SettingsPool.GetValue("input_delay") / 1000f;

            _calibrationTree = calibrationTreeAsset.Instantiate();

            UIManager.Instance.uiDocument.rootVisualElement.Add(_calibrationTree);

            _calibrationTree.style.position = Position.Absolute;
            _calibrationTree.style.top = 0;
            _calibrationTree.style.left = 0;
            _calibrationTree.style.right = 0;
            _calibrationTree.style.bottom = 0;

            _background = _calibrationTree.Q("background");
            _returnButton = _calibrationTree.Q<Button>("return-button");
            _cover = _calibrationTree.Q("cover");
            _hintLabel = _calibrationTree.Q<Label>("hint-label");
            _hintTappedLabel = _calibrationTree.Q<Label>("hint-tapped-label");

            _returnButton.clicked += ClosePanel;

            _cover.RegisterCallbackOnce<PointerUpEvent>(_ => StartCalibration());

            LevelSelectionManager.Instance.songPreviewing = false;

            var volume = SettingsPool.GetValue("volume.song") / 10f;

            _songFadeOutHandle = LMotion.Create(volume, 0, 0.5f)
                .WithOnComplete(() => LevelSelectionManager.Instance.bassHandler.Stop())
                .Bind(x => LevelSelectionManager.Instance.bassHandler.Volume = x);

            _preAnimatedSheet = Resources.Load<StyleSheet>("UI/USS/CalibrationPreAnimted");

            _calibrationTree.styleSheets.Add(_preAnimatedSheet);
            _calibrationTree.AddToClassList("out-animation");
            StartCoroutine(FadeInAnimationRoutine());

            _bassHandler =
                new BassHandler(
                    BetterStreamingAssets.ReadAllBytes("default_sfx/user_interface_sfx/calibration_beat.wav"));

            _bassHandler.Play();
            _bassHandler.Volume = 0;

            return;

            IEnumerator FadeInAnimationRoutine()
            {
                yield return new WaitForSeconds(0.1f);
                _calibrationTree.styleSheets.Remove(_preAnimatedSheet);
                yield return new WaitForSeconds(0.5f);
                _calibrationTree.RemoveFromClassList("out-animation");
                _bassHandler.Stop();
                _bassHandler.Volume = 1;
            }
        }

        private void Update()
        {
            if (_calibrationStarted)
            {
                if (_bassHandler.IsPlaying)
                    _timerInSeconds = (float)_bassHandler.GetPosition();
                else _timerInSeconds += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            _songFadeOutHandle.TryCancel();

            LevelSelectionManager.Instance.songPreviewing = true;

            var volume = SettingsPool.GetValue("volume.song") / 10f;
            var currentVolume = LevelSelectionManager.Instance.bassHandler.Volume;

            LMotion.Create(currentVolume, volume, 0.5f)
                .Bind(x => LevelSelectionManager.Instance.bassHandler.Volume = x);

            if (_calibrationTree != null)
                UIManager.Instance.uiDocument.rootVisualElement.Remove(_calibrationTree);
        }

        private void StartCalibration()
        {
            _returnButton.SetEnabled(false);

            _calibrationStarted = true;

            _bassHandler.Volume = 1;
            _bassHandler.Play();

            _hintLabel.style.display = DisplayStyle.None;
            _hintTappedLabel.style.display = DisplayStyle.Flex;

            _cover.RegisterCallback<PointerDownEvent>(_ => CalibrateOnce());
        }

        private void CalibrateOnce()
        {
            if (!_calibrationStarted)
                return;

            _deltaTimeArray[_beatIndex] = _timerInSeconds - (_beats[_beatIndex] + _inputOffset);

            _hintTappedLabel.text = (int)(_deltaTimeArray[_beatIndex++] * 1000) + "ms";

            Effect();

            if (_beatIndex >= _beats.Length)
            {
                _calibrationStarted = false;

                StartCoroutine(WaitAndClosePanel());
            }

            return;

            IEnumerator WaitAndClosePanel()
            {
                yield return new WaitForSeconds(0.5f);
                var average = 0f;
                _deltaTimeArray.ToList().ForEach(x => average += x);
                average /= _beats.Length;
                var offset = (int)(average * 1000);
                _hintTappedLabel.text = offset + "ms";

                SettingsPool.SetValue("audio_delay", offset);

                SwitchBasedValueManipulator.UpdateValue();

                yield return new WaitForSeconds(0.5f);
                ClosePanel();
            }
        }

        private void Effect()
        {
            _effectHandle.TryCancel();
            _effectHandle = LMotion.Create(Color.white, _originalEffectColor, 2f).WithEase(Ease.Linear)
                .Bind(x => _background.style.backgroundColor = x);
        }

        private void ClosePanel()
        {
            _returnButton.clicked -= ClosePanel;

            StartCoroutine(FadeOutAnimationRoutine());

            _bassHandler.Stop();

            return;

            IEnumerator FadeOutAnimationRoutine()
            {
                _calibrationTree.AddToClassList("out-animation");
                yield return new WaitForSeconds(0.1f);
                _calibrationTree.styleSheets.Add(_preAnimatedSheet);
                yield return new WaitForSeconds(0.5f);
                Destroy(gameObject);
            }
        }
    }
}