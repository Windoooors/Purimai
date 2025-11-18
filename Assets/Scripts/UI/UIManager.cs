using TMPro;
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

            Application.targetFrameRate = 120;
        }
        
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}