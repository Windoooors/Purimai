using System;
using System.Text;
using System.Threading.Tasks;
using Game;
using UI.ChartMetadataLoading;
using UI.InGame;
using UI.LevelSelection;
using UI.Result;
using UI.Settings;
using UI.Settings.Managers;
using UI.Theming;
using UI.TitleScreen;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Logger = Logging.Logger;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        public static Action OnApplicationHasFocus;
        public static Action OnApplicationLoseFocus;

        public FontAsset mainFontAsset;

        [FormerlySerializedAs("uiDocument")] [FormerlySerializedAs("uIDocument")]
        public PanelRenderer panelRenderer;

        public LevelSelectionManager levelSelectionPrefab;
        public SettingsManager settingsPrefab;
        public ResultManager resultPrefab;
        public CircleMaskManager circleMaskPrefab;
        public PauseManager pausePrefab;
        public ModsManager modsPrefab;
        public ThemeUiManager themeUiPrefab;
        public TitleScreenManager titleScreenPrefab;
        public CalibrationManager calibrationPrefab;
        public ChartMetadataLoadingScreenManager chartMetadataLoadingPrefab;

        public ResultManager resultManager;
        public LevelSelectionManager levelSelectionManager;
        public SettingsManager settingsManager;
        public CircleMaskManager circleMaskManager;
        public PauseManager pauseManager;
        public ModsManager modsManager;
        public ThemeUiManager themeUiManager;
        public TitleScreenManager titleScreenManager;
        public CalibrationManager calibrationManager;
        public ChartMetadataLoadingScreenManager chartMetadataLoadingManager;

        public Vector2Int portraitReferenceResolution = new(600, 600);
        public Vector2Int landscapeReferenceResolution = new(1024, 600);

        public static UIManager Instance => _instance ??= FindAnyObjectByType<UIManager>();

        public VisualElement RootElement { get; private set; }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                OnApplicationHasFocus();
            else
                OnApplicationLoseFocus();
        }

        private void OnApplicationQuit()
        {
            GlobalAudioMixer.Free();

            SettingsPool.Save();
        }

        private async void OnUIReload(PanelRenderer renderer, VisualElement rootElement)
        {
            RootElement = rootElement;

            _instance = this;

            ApplyResolution();

            SettingsManager.OnSettingsChanged += ApplyResolution;

            ShowChartMetaLoadingScreen();

            await Task.Delay(300);

            await chartMetadataLoadingManager.Load(() =>
            {
                ShowTitle();

                ScreenOrientationManager.Instance.ScreenChanged += ChangeLayoutConsideringOrientation;

                PlayerPrefs.SetFloat("CalibrationDeltaTimeThreshold", 0.020f);
            });
        }

        public void ShowCalibrationPanel()
        {
            calibrationManager = Instantiate(calibrationPrefab, transform);
            ApplySafeArea();
        }

        public void ShowTitle()
        {
            titleScreenManager = Instantiate(titleScreenPrefab, transform);
            ApplySafeArea();
        }

        public void ShowResult()
        {
            resultManager = Instantiate(resultPrefab, transform);
            ApplySafeArea();
        }

        public void ShowChartMetaLoadingScreen()
        {
            chartMetadataLoadingManager = Instantiate(chartMetadataLoadingPrefab, transform);
            ApplySafeArea();
        }

        public void ShowPausePanel()
        {
            pauseManager = Instantiate(pausePrefab, transform);
            ApplySafeArea();
        }

        public void ShowThemeUiPanel()
        {
            themeUiManager = Instantiate(themeUiPrefab, transform);
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            var root = RootElement;

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
                    panelRenderer.panelSettings.match = 0;
                    panelRenderer.panelSettings.referenceResolution = portraitReferenceResolution;
                    break;
                case ScreenOrientation.LandscapeLeft:
                case ScreenOrientation.LandscapeRight:
                    panelRenderer.panelSettings.match = 1;
                    panelRenderer.panelSettings.referenceResolution = landscapeReferenceResolution;
                    break;
            }

            ApplySafeArea();
        }

        public void UpdateTMPAtlas(char[] characters)
        {
            var missingCharsBuilder = new StringBuilder();
            foreach (var c in characters)
            {
                uint unicode = c;
                if (!mainFontAsset.HasCharacter(unicode)) missingCharsBuilder.Append(c);
            }

            var toAdd = missingCharsBuilder.ToString();
            if (toAdd.Length == 0)
                return;

            try
            {
                mainFontAsset.TryAddCharacters(toAdd);
            }
            catch (ArgumentException)
            {
                Logger.LogError("Batched glyph update failed.");

                foreach (var c in toAdd)
                    try
                    {
                        mainFontAsset.TryAddCharacters(c.ToString());
                    }
                    catch
                    {
                    }
            }
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