using System;
using Coffee.UISoftMask;
using Game;
using TMPro;
using UI.GameSettings;
using UnityEngine;

namespace UI
{
    [Serializable]
    public class ButtonIcons
    {
        public Sprite upArrow;
        public Sprite downArrow;
        public Sprite play;
        public Sprite settings;
        public Sprite levelUp;
        public Sprite levelDown;
        public Sprite back;
        public Sprite sheet;
        public Sprite finale;
        public Sprite dx;
        public Sprite score;
        public Sprite achievement;
        public Sprite retry;
    }

    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        public CanvasGroup maskCanvasGroup;
        public SoftMask mask;
        public Canvas canvas;

        public TMP_FontAsset mainFontAsset;

        public ButtonIcons buttonIcons;

        public VertexGradient fcGoldColorGradient;
        public VertexGradient fcColorGradient;

        public AudioManager.SoundNameData soundFileNameData;

        private void Awake()
        {
            _instance = this;

            ApplyResolution();

            SettingsController.OnSettingsChanged += (_, _) => { ApplyResolution(); };

            AudioManager.GetInstance().LoadAllSoundEffects(soundFileNameData);
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