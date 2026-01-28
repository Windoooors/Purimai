using System;
using System.Threading.Tasks;
using Game;
using Game.ChartManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.LevelSelection
{
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader GetInstance =>
            _instance == null
                ? FindObjectsByType<LevelLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0]
                : _instance;
        
        private static LevelLoader _instance;

        private void Awake()
        {
            _instance = this;
            
            Initialize();
        }

        private Maidata _maidata;
        private int _levelIndex;
        private bool _enteringLevel;

        private void Initialize()
        {
            _resourceLoaded = false;
            _enteringLevel = false;
            _maidata = null;
            _levelIndex = 0;
        }
        
        public void EnterLevel(Maidata maidata, int difficultyIndex)
        {
            _maidata = maidata;
            _levelIndex = difficultyIndex;
            
            StartCoroutine(maidata.LoadSongClip());
            Task.Run(maidata.GenerateBlurredCover);
            
            _enteringLevel = true;
        }

        private bool _resourceLoaded;
        
        private void Update()
        {
            if (!_enteringLevel)
                return;
            
            if (_maidata.SongLoaded && _maidata.BlurredSongCoverGenerated && _maidata.CoverDataLoaded && !_resourceLoaded)
            {
                _resourceLoaded = true;
                
                SceneManager.LoadSceneAsync("Game");
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            ChartPlayer.Instance.InitializeLevel(_maidata, _levelIndex);
            
            Initialize();
        }
    }
}