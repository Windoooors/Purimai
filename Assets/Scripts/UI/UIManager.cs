using Game;
using UI.InGame;
using UI.LevelSelection;
using UI.Result;
using UI.Settings;
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

        public ResultManager resultManager;
        public LevelSelectionManager levelSelectionManager;
        public SettingsManager settingsManager;
        public CircleMaskManager circleMaskManager;
        
        public void ShowResult()
        {
            resultManager = Instantiate(resultPrefab, transform);
            ApplySafeArea();
        }
        
        void ApplySafeArea()
        {
            var root = uiDocument.rootVisualElement;
            
            Rect safeArea = Screen.safeArea;
            
            Vector2 screenSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            
            float left = safeArea.x / screenSize.x * 100f;
            float right = (screenSize.x - safeArea.width - safeArea.x) / screenSize.x * 100f;
            float top = (screenSize.y - safeArea.height - safeArea.y) / screenSize.y * 100f;
            float bottom = safeArea.y / screenSize.y * 100f;
            
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

        private void Awake()
        {
            _instance = this;

            ApplyResolution();
            
            uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                ApplySafeArea();
            });
            
            SettingsManager.OnSettingsChanged += ApplyResolution;
            
            AudioManager.GetInstance().LoadAllSoundEffects(gameSoundFileNameData, uiSoundFileNameData);

            ShowLevelSelector();
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            SettingsPool.Save();
        }

        public static UIManager GetInstance()
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<UIManager>();
            return _instance;
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

            var refreshRateValue = SettingsPool.GetValue("graphics.framerate_limiter");
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