using System;
using System.Collections;
using System.IO;
using Coffee.UISoftMask;
using Game;
using TMPro;
using UI.GameSettings;
using UnityEngine;
using UnityEngine.Networking;

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

        public SoundEffectManager.SoundPathData soundFileNameData;

        private void Awake()
        {
            _instance = this;

            ApplyResolution();

            SettingsController.OnSettingsChanged += (_, _) => { ApplyResolution(); };

            StartCoroutine(GetSoundPath());

            return;

            IEnumerator GetSoundPath()
            {
                var soundPathData = new SoundEffectManager.SoundPathData();

                yield return StartCoroutine(StartDownloadSound(soundFileNameData.perfectSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.greatSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.goodSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.breakExtraSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.breakPerfectSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.breakGreatSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.criticalSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.slideSoundPath));
                yield return StartCoroutine(StartDownloadSound(soundFileNameData.preparatoryBeatSoundPath));

                soundPathData.perfectSoundPath = PathConstructor(soundFileNameData.perfectSoundPath);
                soundPathData.greatSoundPath = PathConstructor(soundFileNameData.greatSoundPath);
                soundPathData.goodSoundPath = PathConstructor(soundFileNameData.goodSoundPath);
                soundPathData.breakExtraSoundPath = PathConstructor(soundFileNameData.breakExtraSoundPath);
                soundPathData.breakPerfectSoundPath = PathConstructor(soundFileNameData.breakPerfectSoundPath);
                soundPathData.breakGreatSoundPath = PathConstructor(soundFileNameData.breakGreatSoundPath);
                soundPathData.slideSoundPath = PathConstructor(soundFileNameData.slideSoundPath);
                soundPathData.criticalSoundPath = PathConstructor(soundFileNameData.criticalSoundPath);
                soundPathData.preparatoryBeatSoundPath = PathConstructor(soundFileNameData.preparatoryBeatSoundPath);

                SoundEffectManager.LoadAllSound(soundPathData);

                yield break;
                
                string PathConstructor(string fileName) =>
                    Path.Combine(Application.persistentDataPath, "DefaultSFX/GameSFX/" + fileName);
            }

            IEnumerator StartDownloadSound(string fileName)
            {
                if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "DefaultSFX/GameSFX/")))
                    Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "DefaultSFX/GameSFX/"));

                var filePath = Path.Combine(Application.persistentDataPath, "DefaultSFX/GameSFX/" + fileName);

                if (File.Exists(filePath)) yield break;

                var uri = new Uri(Path.Combine(Application.streamingAssetsPath, "DefaultSFX/GameSFX/" + fileName));

                var request = UnityWebRequest.Get(uri);

                yield return request.SendWebRequest();

                if (request.downloadHandler.data != null)
                    File.WriteAllBytes(filePath, request.downloadHandler.data);

                request.Dispose();
            }
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