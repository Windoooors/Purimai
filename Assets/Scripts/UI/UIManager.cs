using System;
using Coffee.UISoftMask;
using Game;
using TMPro;
using UI.GameSettings;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        
        public TMP_FontAsset mainFontAsset;

        [FormerlySerializedAs("soundFileNameData")] public AudioManager.GameSoundNameData gameSoundFileNameData;
        public AudioManager.UiSoundNameData  uiSoundFileNameData;

        public UIDocument uIDocument;
        
        public VisualTreeAsset scoreContentVisualTreeAsset;

        private void Awake()
        {
            _instance = this;

            ApplyResolution();

            //SettingsController.OnSettingsChanged += (_, _) => { ApplyResolution(); };

            AudioManager.GetInstance().LoadAllSoundEffects(gameSoundFileNameData, uiSoundFileNameData);
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            EnableUI();
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

        }

        public void DisableUI()
        {

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