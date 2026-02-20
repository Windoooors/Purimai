using System;
using System.Threading.Tasks;
using Game;
using UI.Result;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.LevelSelection
{
    public class LevelLoader : MonoBehaviour
    {
        private static LevelLoader _instance;
        private bool _enteringLevel;
        private int _levelIndex;

        private Maidata _maidata;

        private bool _resourceLoaded;

        public Action PlayerPrefsSavingProcedure;

        public Action SceneLoaded;

        public static LevelLoader Instance =>
            _instance == null
                ? FindObjectsByType<LevelLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None)[^1]
                : _instance;

        private void Awake()
        {
            _instance = this;

            Initialize();
        }

        private void Update()
        {
            if (!_enteringLevel)
                return;

            if (_maidata.SongLoaded && _maidata.BlurredSongCoverGenerated && _maidata.CoverDataLoaded &&
                !_resourceLoaded)
            {
                _resourceLoaded = true;

                SceneManager.LoadSceneAsync("Game");
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void Initialize()
        {
            _resourceLoaded = false;
            _enteringLevel = false;
            _maidata = null;
            _levelIndex = 0;
        }

        public void EnterLevel(Maidata maidata, int difficultyIndex)
        {
            SimulatedSensor.Clear();

            Scoreboard.Reset();

            _maidata = maidata;
            _levelIndex = difficultyIndex;

            maidata.UnloadSong();
            
            StartCoroutine(maidata.LoadSongClip(false));
            Task.Run(maidata.GenerateBlurredCover);

            ScreenOrientationManager.Instance.EnablePortrait();

            PlayerPrefsSavingProcedure?.Invoke();

            _enteringLevel = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            AudioManager.Instance.AudioSourcePool.Clear();

            ChartPlayer.Instance.InitializeLevel(_maidata, _levelIndex);

            Initialize();

            SceneLoaded?.Invoke();

            UIManager.Instance.ShowCircleMask();
        }
    }
}