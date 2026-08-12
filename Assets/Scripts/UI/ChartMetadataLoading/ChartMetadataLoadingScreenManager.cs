using System;
using System.Collections;
using System.Threading.Tasks;
using UI.LevelSelection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.ChartMetadataLoading
{
    public class ChartMetadataLoadingScreenManager : MonoBehaviour
    {
        private static ChartMetadataLoadingScreenManager _instance;
        public VisualTreeAsset loadingVisualTreeAsset;

        private VisualElement _loadingScreenRoot;

        public static ChartMetadataLoadingScreenManager Instance =>
            _instance == null
                ? FindObjectsByType<ChartMetadataLoadingScreenManager>(FindObjectsInactive.Include)[^1]
                : _instance;

        public Label ProgressLabel { get; private set; }

        private void Awake()
        {
            _instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            UIManager.Instance.RootElement?.Remove(_loadingScreenRoot);
        }

        private void Initialize()
        {
            _loadingScreenRoot = loadingVisualTreeAsset.Instantiate();

            UIManager.Instance.RootElement.Add(_loadingScreenRoot);

            _loadingScreenRoot.style.position = new StyleEnum<Position>(Position.Absolute);
            _loadingScreenRoot.style.top = 0;
            _loadingScreenRoot.style.left = 0;
            _loadingScreenRoot.style.bottom = 0;
            _loadingScreenRoot.style.right = 0;

            ProgressLabel = _loadingScreenRoot.Q<Label>("progress-label");

            _loadingScreenRoot.AddToClassList("background-hidden");

            StartCoroutine(ShowLoadingScreen());

            return;

            IEnumerator ShowLoadingScreen()
            {
                yield return new WaitForSeconds(0.1f);
                _loadingScreenRoot.AddToClassList("background-in-animation");
                _loadingScreenRoot.RemoveFromClassList("background-hidden");
                yield return new WaitForSeconds(0.2f);
            }
        }

        public async Task Load(Action onLoadComplete, bool clear = false)
        {
            var progress = new Progress<MaidataManager.LoadProgressReport>(report =>
            {
                if (ProgressLabel != null) ProgressLabel.text = $"{report.Percentage * 100:0.00}%";
            });

            await MaidataManager.LoadAsync(progress, clear);

            StartCoroutine(ShowLoadingScreen());

            return;

            IEnumerator ShowLoadingScreen()
            {
                yield return new WaitForSeconds(0.1f);
                _loadingScreenRoot.AddToClassList("background-in-animation");
                _loadingScreenRoot.AddToClassList("background-hidden");
                yield return new WaitForSeconds(0.2f);
                onLoadComplete();

                Destroy(this);
            }
        }
    }
}