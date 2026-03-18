using System.Collections;
using Game.Theming;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UI.Theming
{
    public class ThemeUiListHelper : MonoBehaviour
    {
        public VisualTreeAsset themeSelectorTreeAsset;
        public VisualTreeAsset itemTreeAsset;

        public float itemHeight = 65;

        protected void SetUpList(ListView listView, ThemeData[] items)
        {
            listView.fixedItemHeight = itemHeight;

            var isZh = LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("zh");

            listView.makeItem += () => itemTreeAsset.Instantiate();

            listView.pickingMode = PickingMode.Ignore;

            listView.selectionType = SelectionType.None;

            var scrollView = listView.Q<ScrollView>();

            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;

            listView.bindItem += (element, i) =>
            {
                var userData = new ItemUserData();

                var itemData = items[i];

                var elementRoot = element.Q<VisualElement>("settings-item");

                var displayName = isZh ? itemData.themeDataDto.DisplayNameZh : itemData.themeDataDto.DisplayNameEn;
                elementRoot.Q<Label>("item-title").text = displayName;
                elementRoot.Q<Label>("item-name-watermark").text =
                    $"<color=white><gradient=\"level-item-watermark\">{displayName}</gradient></color>";

                var description = isZh ? itemData.themeDataDto.DescriptionZh : itemData.themeDataDto.DescriptionEn;

                elementRoot.Q<Label>("item-description").text = description;

                VisualElement valuePanel = themeSelectorTreeAsset.Instantiate();

                elementRoot.Add(valuePanel);
                userData.ThemeManipulator =
                    new ThemeSelectorManipulator(itemData);
                valuePanel.Q("theme-selector").AddManipulator(userData.ThemeManipulator);

                userData.ThemeSelector = valuePanel;

                element.userData = userData;
            };

            listView.unbindItem += (element, _) =>
            {
                var userData = (ItemUserData)element.userData;

                userData.ThemeSelector.Q("theme-selector")
                    .RemoveManipulator(userData.ThemeManipulator);

                var elementRoot = element.Q<VisualElement>("settings-item");
                elementRoot.Remove(userData.ThemeSelector);
            };

            listView.itemsSource = items;
        }

        private class ItemUserData
        {
            public Manipulator ThemeManipulator;
            public VisualElement ThemeSelector;
        }
    }

    public class ThemeUiManager : ThemeUiListHelper
    {
        private static ThemeUiManager _instance;

        public VisualTreeAsset themeSelectorPanelTreeAsset;

        private VisualElement _root;
        private VisualElement _themeSelectorPanelTree;

        public static ThemeUiManager Instance => _instance ??= FindAnyObjectByType<ThemeUiManager>();

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UIManager.Instance.uiDocument?.rootVisualElement?.Remove(_themeSelectorPanelTree);
        }

        private void Initialize()
        {
            _root = UIManager.Instance.uiDocument.rootVisualElement;

            _themeSelectorPanelTree = themeSelectorPanelTreeAsset.Instantiate();

            _themeSelectorPanelTree.style.position = Position.Absolute;
            _themeSelectorPanelTree.style.left = 0;
            _themeSelectorPanelTree.style.right = 0;
            _themeSelectorPanelTree.style.top = 0;
            _themeSelectorPanelTree.style.bottom = 0;

            _root.Add(_themeSelectorPanelTree);

            var clickablePanel = _themeSelectorPanelTree.Q("transparent-panel").Q("clickable-panel");

            clickablePanel.RegisterCallback<PointerUpEvent>(_ => { ClosePanel(); });

            var listView = _themeSelectorPanelTree.Q<ListView>();

            listView.TrySetTouchDraggingAllowed(true);

            SetUpList(listView, ThemeManager.SkinDataList.ToArray());

            StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            yield return new WaitForSeconds(0.1f);
            _themeSelectorPanelTree.style.display = DisplayStyle.Flex;
            _themeSelectorPanelTree.AddToClassList("shown");
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

        private void ClosePanel()
        {
            _themeSelectorPanelTree.RemoveFromClassList("shown");
            _themeSelectorPanelTree.AddToClassList("hidden");

            StartCoroutine(ClosePanelRoutine());

            return;

            IEnumerator ClosePanelRoutine()
            {
                yield return new WaitForSeconds(0.5f);

                ThemeApplier.Instance.LoadTheme();

                DestroySelf();
            }
        }
    }
}