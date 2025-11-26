using TMPro;
using UI.GameSettings;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        public CanvasGroup maskCanvasGroup;
        public Canvas canvas;

        public TMP_FontAsset mainFontAsset;

        private void Awake()
        {
            _instance = this;

            ApplyResolution();

            SettingsController.OnSettingsChanged += (_, _) => { ApplyResolution(); };
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
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

        public void EnableUI()
        {
            canvas.gameObject.SetActive(true);
            maskCanvasGroup.alpha = 1;
        }

        public void DisableUI()
        {
            canvas.gameObject.SetActive(false);
            maskCanvasGroup.alpha = 0;
        }

        private void ApplyResolution()
        {
#if !(UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
            var maxRefreshRate = Screen.currentResolution.refreshRateRatio;
#endif

            var refreshRateValue = SettingsPool.GetValue("general.framerate_limiter");
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