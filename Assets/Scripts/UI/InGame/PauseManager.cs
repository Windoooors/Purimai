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

            UIManager.GetInstance().uiDocument.rootVisualElement.Add(_pausePanelTree);

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
            UIManager.GetInstance().uiDocument.rootVisualElement.Remove(_pausePanelTree);
        }

        private void GoToMenu()
        {
            StartCoroutine(StartGoToMenuAnimation());

            return;

            IEnumerator StartGoToMenuAnimation()
            {
                _pausePanelTree.AddToClassList("in-animation");

                _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/PauseOutAnimated"));

                yield return new WaitForSeconds(0.5f);

                SimulatedSensor.Enabled = true;

                Destroy(UIManager.GetInstance().circleMaskManager.gameObject);

                ChartPlayer.Instance.Maidata.UnloadResources();

                SimulatedSensor.Clear();

                AudioManager.GetInstance().AudioSourcePool.Clear();

                SceneManager.LoadScene("Empty");

                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;

                UIManager.GetInstance().ShowLevelSelector();

                _pausePanelTree.BringToFront();

                StartCoroutine(StartFadeOutAnimation());
            }

            IEnumerator StartFadeOutAnimation()
            {
                yield return new WaitForSeconds(0.1f);

                _pausePanelTree.RemoveFromClassList("in-animation");
                _pausePanelTree.AddToClassList("out-animation");

                _pausePanelTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Pause/PauseOutToGameAnimated"));

                yield return new WaitForSeconds(0.5f);

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

                yield return new WaitForSeconds(0.5f);

                CircleMaskManager.GetInstance.Resume();

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

                Destroy(UIManager.GetInstance().circleMaskManager.gameObject);

                LevelLoader.GetInstance.EnterLevel(ChartPlayer.Instance.Maidata,
                    ChartPlayer.Instance.levelDifficultyIndex);

                LevelLoader.GetInstance.SceneLoaded += SceneLoaded;
            }

            void SceneLoaded()
            {
                LevelLoader.GetInstance.SceneLoaded -= SceneLoaded;

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