using Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.InGame
{
    public class CircleMaskManager : MonoBehaviour
    {
        private static CircleMaskManager _instance;
        public VisualTreeAsset circleMaskTreeAsset;
        private VisualElement _circleMaskTree;

        private bool _paused;

        public static CircleMaskManager Instance => _instance ?? FindAnyObjectByType<CircleMaskManager>();

        private void Awake()
        {
            _circleMaskTree = circleMaskTreeAsset.Instantiate();

            _circleMaskTree.style.position = Position.Absolute;
            _circleMaskTree.style.top = 0;
            _circleMaskTree.style.left = 0;
            _circleMaskTree.style.right = 0;
            _circleMaskTree.style.bottom = 0;

            UIManager.Instance.uiDocument.rootVisualElement.Add(_circleMaskTree);

            _instance = this;

            _circleMaskTree.SendToBack();

            _circleMaskTree.Q<Button>("pause-button").clicked += Pause;
        }

        private void OnDestroy()
        {
            UIManager.Instance.uiDocument.rootVisualElement.Remove(_circleMaskTree);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Pause();
        }

        private void Pause()
        {
            if (_paused)
                return;

            ChartPlayer.Instance.Pause(out var succeed);

            if (!succeed)
                return;

            _paused = true;

            _circleMaskTree.AddToClassList("hide-button");

            UIManager.Instance.ShowPausePanel();
        }

        public void Resume()
        {
            if (!_paused)
                return;

            _circleMaskTree.RemoveFromClassList("hide-button");

            ChartPlayer.Instance.Resume();

            _paused = false;
        }
    }
}