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
        }
        
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}