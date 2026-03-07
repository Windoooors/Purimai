using System.Collections;
using Game;
using UI.LevelSelection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.InGame
{
    public class PauseManager : MonoBehaviour
    {
        public VisualTreeAsset pausePanelTreeAsset;

        private VisualElement _pausePanelTree;

        private void Awake()
        {
            _pausePanelTree = pausePanelTreeAsset.Instantiate();

            UIManager.Instance.uiDocument.rootVisualElement.Add(_pausePanelTree);

            _pausePanelTree.style.position = Position.Absolute;
            _pausePanelTree.style.top = 0;
            _pausePanelTree.style.left = 0;
            _pausePanelTree.style.right = 0;
            _pausePanelTree.style.bottom = 0;

            _pausePanelTree.Q<Button>("retry-button").clicked += Retry;
            _pausePanelTree.Q<Button>("back-button").clicked += Resume;
            _pausePanelTree.Q<Button>("level-selection-button").clicked += GoToMenu;

            StartCoroutine(StartInAnimation());
        }

        private void OnDestroy()
        {
            UIManager.Instance.uiDocument.rootVisualElement.Remove(_pausePanelTree);
        }

        private void GoToMenu()
        {
            var levelSelectionPreAnimatedSheet =
                Resources.Load<StyleSheet>("UI/USS/LevelSelection/PauseToLevelSelectionPreAnimated");

            StartCoroutine(StartGoToMenuAnimation());

            return;

            IEnumerator StartGoToMenuAnimation()
            {
                _pausePanelTree.AddToClassList("in-animation");

                _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/PauseOutAnimated"));

                yield return new WaitForSeconds(0.5f);

                SimulatedSensor.Enabled = true;

                Destroy(UIManager.Instance.circleMaskManager.gameObject);

                ChartPlayer.Instance.Maidata.UnloadResources();

                SimulatedSensor.Clear();

                SceneManager.LoadScene("Empty");

                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;

                UIManager.Instance.ShowLevelSelector();

                LevelSelectionManager.Instance.LevelSelectionTree.styleSheets.Add(levelSelectionPreAnimatedSheet);

                _pausePanelTree.BringToFront();

                StartCoroutine(StartFadeOutAnimation());
            }

            IEnumerator StartFadeOutAnimation()
            {
                yield return new WaitForSeconds(0.1f);

                _pausePanelTree.RemoveFromClassList("in-animation");
                _pausePanelTree.AddToClassList("out-animation");

                LevelSelectionManager.Instance.LevelSelectionTree.AddToClassList("out-animation");

                _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/PauseOutToGameAnimated"));

                LevelSelectionManager.Instance.LevelSelectionTree.styleSheets.Remove(levelSelectionPreAnimatedSheet);

                yield return new WaitForSeconds(0.5f);

                LevelSelectionManager.Instance.LevelSelectionTree.RemoveFromClassList("out-animation");

                RemoveSelf();
            }
        }

        private IEnumerator StartInAnimation()
        {
            var preAnimatedSheet = Resources.Load<StyleSheet>("UI/USS/Pause/GameToPausePreAnimated");

            _pausePanelTree.styleSheets.Add(preAnimatedSheet);

            yield return new WaitForSeconds(0.1f);

            _pausePanelTree.AddToClassList("out-animation");

            _pausePanelTree.styleSheets.Remove(preAnimatedSheet);

            yield return new WaitForSeconds(0.5f);

            _pausePanelTree.RemoveFromClassList("out-animation");
        }

        private void Resume()
        {
            StartCoroutine(StartResumeAnimation());

            return;

            IEnumerator StartResumeAnimation()
            {
                _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/GameToPausePreAnimated"));
                _pausePanelTree.AddToClassList("out-animation");

                ChartPlayer.Instance.Resume();

                yield return new WaitForSeconds(0.5f);

                CircleMaskManager.Instance.Resume();

                RemoveSelf();
            }
        }

        private void Retry()
        {
            StartCoroutine(StartRetryAnimation());

            return;

            IEnumerator StartRetryAnimation()
            {
                _pausePanelTree.AddToClassList("in-animation");

                _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/PauseOutAnimated"));

                yield return new WaitForSeconds(0.5f);

                SimulatedSensor.Enabled = true;

                Destroy(UIManager.Instance.circleMaskManager.gameObject);

                LevelLoader.Instance.EnterLevel(ChartPlayer.Instance.Maidata,
                    ChartPlayer.Instance.levelDifficultyIndex);

                LevelLoader.Instance.SceneLoaded += SceneLoaded;
            }

            void SceneLoaded()
            {
                LevelLoader.Instance.SceneLoaded -= SceneLoaded;

                StartCoroutine(WaitAndDestroySelf());

                return;

                IEnumerator WaitAndDestroySelf()
                {
                    yield return new WaitForSeconds(0.1f);

                    _pausePanelTree.RemoveFromClassList("in-animation");
                    _pausePanelTree.AddToClassList("out-animation");
                    _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/PauseOutToGameAnimated"));

                    yield return new WaitForSeconds(0.5f);

                    RemoveSelf();
                }
            }
        }

        private void RemoveSelf()
        {
            Destroy(gameObject);
        }
    }
}