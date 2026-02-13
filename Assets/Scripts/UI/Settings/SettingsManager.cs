using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UI.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static Action OnSettingsChanged;

        private static SettingsManager _instance;

        public VisualTreeAsset settingsTreeAsset;
        public VisualTreeAsset tabContentTreeAsset;

        [FormerlySerializedAs("valueControllerTreeAsset")]
        public VisualTreeAsset switchBasedValueControllerTreeAsset;

        public VisualTreeAsset toggleBasedValueControllerTreeAsset;
        public VisualTreeAsset itemTreeAsset;

        private VisualElement _root;

        private VisualElement _settingsTree;
        private TabView _tabView;

        public static SettingsManager GetInstance => _instance ??=
            FindObjectsByType<SettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)[^1];

        private void Awake()
        {
            _instance = this;

            Initialize();
        }

        private void OnDestroy()
        {
            UIManager.GetInstance().uiDocument?.rootVisualElement?.Remove(_settingsTree);
        }

        private void Initialize()
        {
            _root = UIManager.GetInstance().uiDocument.rootVisualElement;

            _settingsTree = settingsTreeAsset.Instantiate();

            _settingsTree.style.position = Position.Absolute;
            _settingsTree.style.left = 0;
            _settingsTree.style.right = 0;
            _settingsTree.style.top = 0;
            _settingsTree.style.bottom = 0;

            _root.Add(_settingsTree);

            _tabView = _settingsTree.Q("transparent-panel").Q<TabView>();

            var clickablePanel = _settingsTree.Q("transparent-panel").Q("clickable-panel");

            clickablePanel.RegisterCallback<PointerUpEvent>(e =>
            {
                SettingsPool.Save();

                OnSettingsChanged?.Invoke();

                ClosePanel();
            });

            foreach (var category in SettingsItems.Settings)
            {
                var tab = new Tab();

                var titleLocalizedString = new LocalizedString("UI", $"settings.{category.Identifier}");

                titleLocalizedString.StringChanged += value => { tab.label = value; };

                titleLocalizedString.RefreshString();

                var tabContent = tabContentTreeAsset.Instantiate();

                tab.Add(tabContent);

                tabContent.style.position = Position.Absolute;
                tabContent.style.top = 0;
                tabContent.style.bottom = 0;
                tabContent.style.left = 0;
                tabContent.style.right = 0;

                var listView = tabContent.Q<ListView>();

                listView.makeItem += () => itemTreeAsset.Instantiate();

                listView.fixedItemHeight = 125;

                listView.pickingMode = PickingMode.Ignore;

                listView.selectionType = SelectionType.None;

                var scrollView = listView.Q<ScrollView>();

                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

                scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;

                listView.bindItem += (element, i) =>
                {
                    var userData = new ItemUserData();

                    var itemData = category.Items[i];

                    var elementRoot = element.Q<VisualElement>("settings-item");

                    var itemTitleLocalizedString = new LocalizedString("UI", $"settings.{itemData.Identifier}");

                    itemTitleLocalizedString.StringChanged += value =>
                    {
                        elementRoot.Q<Label>("item-title").text = value;
                        elementRoot.Q<Label>("item-name-watermark").text =
                            $"<color=white><gradient=\"level-item-watermark\">{value}</gradient></color>";
                    };

                    itemTitleLocalizedString.RefreshString();

                    userData.LocalizedString = itemTitleLocalizedString;

                    VisualElement valuePanel = null;

                    switch (itemData.ValueSet)
                    {
                        case SeparatedValueSet:
                        case SuccessiveIntegerValueSet:
                            valuePanel = switchBasedValueControllerTreeAsset.Instantiate();
                            elementRoot.Add(valuePanel);
                            userData.ValueManipulator = new SwitchBasedValueManipulator(itemData.Identifier);
                            valuePanel.Q("selector-based-value-panel").AddManipulator(userData.ValueManipulator);
                            break;
                        case BoolValueSet:
                            valuePanel = toggleBasedValueControllerTreeAsset.Instantiate();
                            elementRoot.Add(valuePanel);
                            userData.ValueManipulator = new ToggleBasedValueManipulator(itemData.Identifier);
                            valuePanel.Q("toggle-based-value-panel").AddManipulator(userData.ValueManipulator);
                            break;
                    }

                    userData.ValuePanel = valuePanel;

                    element.userData = userData;
                };

                listView.unbindItem += (element, i) =>
                {
                    var userData = (ItemUserData)element.userData;

                    var itemData = category.Items[i];

                    switch (itemData.ValueSet)
                    {
                        case SeparatedValueSet:
                        case SuccessiveIntegerValueSet:
                            userData.ValuePanel.Q("selector-based-value-panel")
                                .RemoveManipulator(userData.ValueManipulator);
                            break;
                        case BoolValueSet:
                            userData.ValuePanel.Q("toggle-based-value-panel")
                                .RemoveManipulator(userData.ValueManipulator);
                            break;
                    }

                    var elementRoot = element.Q<VisualElement>("settings-item");
                    elementRoot.Remove(userData.ValuePanel);
                    userData.LocalizedString.Clear();
                };

                _tabView.contentViewport.BringToFront();

                listView.itemsSource = category.Items;

                _tabView.Add(tab);
            }

            _settingsTree.style.display = DisplayStyle.None;

            Invoke(nameof(Show), 0.05f);
        }

        private void Show()
        {
            _settingsTree.style.display = DisplayStyle.Flex;
            _settingsTree.AddToClassList("shown");
        }

        private void ClosePanel()
        {
            _settingsTree.RemoveFromClassList("shown");
            _settingsTree.AddToClassList("hidden");

            Invoke(nameof(DestroySelf), 0.5f);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

        private class ItemUserData
        {
            public LocalizedString LocalizedString;

            public Manipulator ValueManipulator;
            public VisualElement ValuePanel;
        }
    }
}