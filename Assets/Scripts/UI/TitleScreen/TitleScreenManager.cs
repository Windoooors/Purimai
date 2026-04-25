using System.Collections;
using UI.LevelSelection;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UI.TitleScreen
{
    public class TitleScreenManager : MonoBehaviour
    {
        private static TitleScreenManager _instance;

        public VisualTreeAsset titleScreenTreeAsset;

        private Label _hintLabel;

        private StyleSheet _preAnimatedSheet;
        private VisualElement _titleScreenTree;
        private VisualElement _touchCover;
        private Label _versionLabel;
        public static TitleScreenManager Instance => _instance ??= FindAnyObjectByType<TitleScreenManager>();

        private void Awake()
        {
            _instance = this;

            Initialize();
        }

        private void OnDestroy()
        {
            if (_titleScreenTree != null)
                UIManager.Instance.uiDocument.rootVisualElement.Remove(_titleScreenTree);
        }

        private void Initialize()
        {
            _titleScreenTree = titleScreenTreeAsset.Instantiate();

            UIManager.Instance.uiDocument.rootVisualElement.Add(_titleScreenTree);

            _titleScreenTree.style.position = Position.Absolute;
            _titleScreenTree.style.top = 0;
            _titleScreenTree.style.left = 0;
            _titleScreenTree.style.right = 0;
            _titleScreenTree.style.bottom = 0;

            _hintLabel = _titleScreenTree.Q<Label>("hint-label");
            _touchCover = _titleScreenTree.Q<VisualElement>("cover");

            var noCharts = MaidataManager.MaidataList.Count == 0;

            var localizedString = new LocalizedString();
            localizedString.SetReference("UI.LevelSelection", noCharts ? "title.hint_no_charts" : "title.hint");

            localizedString.StringChanged += s => { _hintLabel.text = s; };

            localizedString.RefreshString();

            if (!noCharts)
                _touchCover.RegisterCallbackOnce<PointerUpEvent>(_ => { LoadLevelSelector(); });

            _titleScreenTree.styleSheets.Add(_preAnimatedSheet =
                Resources.Load<StyleSheet>("UI/USS/Title/TitlePreAnimated"));

            _versionLabel = _titleScreenTree.Q<Label>("version-label");

            _versionLabel.text = $"v{Application.version} (Build GUID: {Application.buildGUID})";

            StartCoroutine(TitleInAnimationRoutine());

            return;

            IEnumerator TitleInAnimationRoutine()
            {
                yield return new WaitForSeconds(0.1f);

                _titleScreenTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Title/TitleAnimated"));

                yield return new WaitForSeconds(0.8f);

                _titleScreenTree.styleSheets.Remove(_preAnimatedSheet);
            }
        }

        private void LoadLevelSelector()
        {
            UIManager.Instance.ShowLevelSelector();
            _titleScreenTree.BringToFront();

            var outAnimatedStyleSheet =
                Resources.Load<StyleSheet>("UI/USS/LevelSelection/LevelSelectionToGameInAnimated");

            LevelSelectionManager.Instance.LevelSelectionTree.styleSheets.Add(
                outAnimatedStyleSheet);

            LevelSelectionManager.Instance.LevelSelectionTree.AddToClassList("out-animation");

            StartCoroutine(TitleOutAnimationRoutine());

            return;

            IEnumerator TitleOutAnimationRoutine()
            {
                yield return new WaitForSeconds(0.5f);

                _titleScreenTree.styleSheets.Add(Resources.Load<StyleSheet>("UI/USS/Title/TitleAnimatedOut"));
                yield return new WaitForSeconds(0.5f);

                LevelSelectionManager.Instance.LevelSelectionTree.styleSheets.Remove(outAnimatedStyleSheet);

                yield return new WaitForSeconds(0.5f);

                LevelSelectionManager.Instance.LevelSelectionTree.RemoveFromClassList("out-animation");

                Destroy(gameObject);
            }
        }
    }
}