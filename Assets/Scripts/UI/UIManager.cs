using UI.GameSettings;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        public Canvas canvas;

        private void Awake()
        {
            Instance = this;

            ApplyResolution();

            SettingsController.OnSettingsChanged += (_, _) => { ApplyResolution(); };
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
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