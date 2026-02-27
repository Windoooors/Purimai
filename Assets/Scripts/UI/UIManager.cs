using Game;
using UI.InGame;
using UI.LevelSelection;
using UI.Result;
using UI.Settings;
using UI.Settings.Managers;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        public FontAsset mainFontAsset;

        [FormerlySerializedAs("soundFileNameData")]
        public AudioManager.GameSoundNameData gameSoundFileNameData;

        public AudioManager.UiSoundNameData uiSoundFileNameData;

        [FormerlySerializedAs("uIDocument")] public UIDocument uiDocument;

        public LevelSelectionManager levelSelectionPrefab;
        public SettingsManager settingsPrefab;
        public ResultManager resultPrefab;
        public CircleMaskManager circleMaskPrefab;
        public PauseManager pausePrefab;
        public ModsManager modsPrefab;

        public ResultManager resultManager;
        public LevelSelectionManager levelSelectionManager;
        public SettingsManager settingsManager;
        public CircleMaskManager circleMaskManager;
        public PauseManager pauseManager;
        public ModsManager modsManager;

        public Vector2Int portraitReferenceResolution = new(600, 600);
        public Vector2Int landscapeReferenceResolution = new(1024, 600);

        public static UIManager Instance => _instance ?? FindAnyObjectByType<UIManager>();

        private void Awake()
        {
            _instance = this;

            ApplyResolution();

            uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(evt => { ApplySafeArea(); });

            SettingsManager.OnSettingsChanged += ApplyResolution;

            ShowLevelSelector();

            ScreenOrientationManager.Instance.ScreenChanged += ChangeLayoutConsideringOrientation;
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            SettingsPool.Save();
        }

        public void ShowResult()
        {
            resultManager = Instantiate(resultPrefab, transform);
            ApplySafeArea();
        }

        public void ShowPausePanel()
        {
            pauseManager = Instantiate(pausePrefab, transform);
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            var root = uiDocument.rootVisualElement;

            var safeArea = Screen.safeArea;

            var screenSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

            var left = safeArea.x / screenSize.x * 100f;
            var right = (screenSize.x - safeArea.width - safeArea.x) / screenSize.x * 100f;
            var top = (screenSize.y - safeArea.height - safeArea.y) / screenSize.y * 100f;
            var bottom = safeArea.y / screenSize.y * 100f;

            root.Query<VisualElement>(className: "safe-area").ForEach(x =>
            {
                x.style.left = Length.Percent(left);
                x.style.top = Length.Percent(top);
                x.style.right = Length.Percent(right);
                x.style.bottom = Length.Percent(bottom);
            });

            root.Query<VisualElement>(className: "safe-area-ignore-bottom").ForEach(x =>
            {
                x.style.left = Length.Percent(left);
                x.style.top = Length.Percent(top);
                x.style.right = Length.Percent(right);
            });
        }

        public void ShowLevelSelector()
        {
            levelSelectionManager = Instantiate(levelSelectionPrefab, transform);
            ApplySafeArea();
        }

        public void ShowModsPanel()
        {
            modsManager = Instantiate(modsPrefab, transform);
            ApplySafeArea();
        }

        public void ShowSettingsPanel()
        {
            settingsManager = Instantiate(settingsPrefab, transform);
            ApplySafeArea();
        }

        public void ShowCircleMask()
        {
            circleMaskManager = Instantiate(circleMaskPrefab, transform);
            ApplySafeArea();
        }

        private void ChangeLayoutConsideringOrientation()
        {
            var orientation = Screen.orientation;

            switch (orientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    uiDocument.panelSettings.match = 0;
                    uiDocument.panelSettings.referenceResolution = portraitReferenceResolution;
                    break;
                case ScreenOrientation.LandscapeLeft:
                case ScreenOrientation.LandscapeRight:
                    uiDocument.panelSettings.match = 1;
                    uiDocument.panelSettings.referenceResolution = landscapeReferenceResolution;
                    break;
            }
        }

        public void UpdateTMPAtlas(char[] characters)
        {
            var characterString = new string(characters);
            mainFontAsset.TryAddCharacters(characterString);
        }

        private void ApplyResolution()
        {
#if !(UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
            var maxRefreshRate = Screen.currentResolution.refreshRateRatio;
#endif

            var refreshRateValue = SettingsPool.GetValue("framerate_limiter");
            //var vsyncValue = SettingsPool.GetValue("general.vsync");

            QualitySettings.vSyncCount = 0;

            Application.targetFrameRate = refreshRateValue switch
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
                0 => 0,
#else
                0 => (int)maxRefreshRate.value,
#endif
                _ => 60
            };
        }
    }
}